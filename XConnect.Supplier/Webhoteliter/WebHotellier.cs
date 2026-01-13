using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Webhoteliter
{
    public class WebHotellier
    {
        public class WebHotellierAvailResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Data
            {
                public List<Hotel> hotels { get; set; }
            }

            public class Hotel
            {
                public string code { get; set; }
                public string name { get; set; }
                public string url { get; set; }
                public int rating { get; set; }
                public string currency { get; set; }
                public string avlurl { get; set; }
                public string infourl { get; set; }
                public string bookurl { get; set; }
                public double minprice { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public string distance { get; set; }
                public Location location { get; set; }
                public string type { get; set; }
                public List<Rate> rates { get; set; }
            }

            public class Label
            {
                public string code { get; set; }
                public string title { get; set; }
            }

            public class Location
            {
                public double lat { get; set; }
                public double lon { get; set; }
                public string name { get; set; }
            }

            public class Param
            {
                public string type { get; set; }
                public string name { get; set; }
                public string value { get; set; }
            }

            public class Rate
            {
                public int id { get; set; }
                public string type { get; set; }
                public int board { get; set; }
                public string url { get; set; }
                public string room { get; set; }
                public string roomurl { get; set; }
                public int remaining { get; set; }
                public string rate { get; set; }
                public string rate_desc { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public double stay { get; set; }
                public double extras { get; set; }
                public double taxes { get; set; }
                public double excluded_charges { get; set; }
                public double price { get; set; }
                public double retail { get; set; }
                public double discount { get; set; }
                public double margin { get; set; }
                public string payment_policy { get; set; }
                public int payment_policy_id { get; set; }
                public string cancellation_policy { get; set; }
                public int cancellation_policy_id { get; set; }
                public string cancellation_penalty { get; set; }
                public object cancellation_expiry { get; set; }
                public List<Label> labels { get; set; }
            }

            public class Root
            {
                public string request_id { get; set; }
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<Param> @params { get; set; }
                public Data data { get; set; }
            }


        }

        public class WebHotelierRoomResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class CancellationFee
            {
                public DateTime after { get; set; }
                public double fee { get; set; }
            }

            public class Data
            {
                public string code { get; set; }
                public string name { get; set; }
                public string currency { get; set; }
                public Url url { get; set; }
                public Location location { get; set; }
                public List<Rate> rates { get; set; }
                public List<Taxis> taxes { get; set; }
            }

            public class Location
            {
                public double lat { get; set; }
                public double lon { get; set; }
            }

            public class Param
            {
                public string type { get; set; }
                public string name { get; set; }
                public string value { get; set; }
            }

            public class Payment
            {
                public DateTime due { get; set; }
                public double amount { get; set; }
            }

            public class Pricing
            {
                public double stay { get; set; }
                public int extras { get; set; }
                public double taxes { get; set; }
                public double excluded_charges { get; set; }
                public double price { get; set; }
                public double margin { get; set; }
            }

            public class Rate
            {
                public int id { get; set; }
                public string type { get; set; }
                public string room { get; set; }
                public string rate { get; set; }
                public string rate_desc { get; set; }
                public string payment_policy { get; set; }
                public int payment_policy_id { get; set; }
                public string cancellation_policy { get; set; }
                public int cancellation_policy_id { get; set; }
                public string cancellation_penalty { get; set; }
                public int remaining { get; set; }
                public object cancellation_expiry { get; set; }
                public int board { get; set; }
                public Url url { get; set; }
                public Pricing pricing { get; set; }
                public Retail retail { get; set; }
                public List<Taxis> taxes { get; set; }
                public string status { get; set; }
                public string status_descr { get; set; }
                public List<Payment> payments { get; set; }
                public List<CancellationFee> cancellation_fees { get; set; }
            }

            public class Retail
            {
                public double stay { get; set; }
                public int extras { get; set; }
                public double taxes { get; set; }
                public double excluded_charges { get; set; }
                public double price { get; set; }
                public double discount { get; set; }
            }

            public class Root
            {
                public string request_id { get; set; }
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<Param> @params { get; set; }
                public Data data { get; set; }
            }

            public class Taxis
            {
                public int tax_id { get; set; }
                public double value { get; set; }
                public int order { get; set; }
                public string description { get; set; }
                public int included { get; set; }
                public int per_room { get; set; }
                public int per_day { get; set; }
                public int per_adult { get; set; }
                public int per_child { get; set; }
                public int per_infant { get; set; }
                public string fromd { get; set; }
                public string tod { get; set; }
                public string type { get; set; }
            }

            public class Url
            {
                public string website { get; set; }
                public string info { get; set; }
                public string engine { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public string rate { get; set; }
                public string room { get; set; }
            }


        }

        public class WebHotelierBookResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Data
            {
                public string summaryUrl { get; set; }
                public int res_id { get; set; }
                public string email { get; set; }
                public string result { get; set; }
            }

            public class Root
            {
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<object> @params { get; set; }
                public Data data { get; set; }
            }



        }

        public class WebhotelierCancelResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Data
            {
                public string result { get; set; }
                public double cancellation_penalty_amount { get; set; }
                public string cancellation_penalty_currency { get; set; }
            }

            public class Root
            {
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<object> @params { get; set; }
                public Data data { get; set; }
            }


        }
    }


    public class WebHotellierRoom
    {
        public class WebHotellierAvailResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Data
            {
                public List<Hotel> hotels { get; set; }
            }

            public class Hotel
            {
                public string code { get; set; }
                public string name { get; set; }
                public string url { get; set; }
                public int rating { get; set; }
                public string currency { get; set; }
                public string avlurl { get; set; }
                public string infourl { get; set; }
                public string bookurl { get; set; }
                public double minprice { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public string distance { get; set; }
                public Location location { get; set; }
                public string type { get; set; }
                public List<Rate> rates { get; set; }
            }

            public class Label
            {
                public string code { get; set; }
                public string title { get; set; }
            }

            public class Location
            {
                public double lat { get; set; }
                public double lon { get; set; }
                public string name { get; set; }
            }

            public class Param
            {
                public string type { get; set; }
                public string name { get; set; }
                public string value { get; set; }
            }

            public class Rate
            {
                public int id { get; set; }
                public string type { get; set; }
                public int board { get; set; }
                public string url { get; set; }
                public string room { get; set; }
                public string roomurl { get; set; }
                public int remaining { get; set; }
                public string rate { get; set; }
                public string rate_desc { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public double stay { get; set; }
                public int extras { get; set; }
                public double taxes { get; set; }
                public double excluded_charges { get; set; }
                public double price { get; set; }
                public double retail { get; set; }
                public double discount { get; set; }
                public double margin { get; set; }
                public string payment_policy { get; set; }
                public int payment_policy_id { get; set; }
                public string cancellation_policy { get; set; }
                public int cancellation_policy_id { get; set; }
                public string cancellation_penalty { get; set; }
                public object cancellation_expiry { get; set; }
                public List<Label> labels { get; set; }
            }

            public class Root
            {
                public string request_id { get; set; }
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<Param> @params { get; set; }
                public Data data { get; set; }
            }


        }

        public class WebHotelierRoomResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class CancellationFee
            {
                public DateTime after { get; set; }
                public double fee { get; set; }
            }

            public class Data
            {
                public List<Hotel> hotels { get; set; }
            }

            public class Hotel
            {
                public string code { get; set; }
                public string name { get; set; }
                public string url { get; set; }
                public int rating { get; set; }
                public string currency { get; set; }
                public string avlurl { get; set; }
                public string infourl { get; set; }
                public string bookurl { get; set; }
                public double minprice { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public string distance { get; set; }
                public Location location { get; set; }
                public string type { get; set; }
                public List<Rate> rates { get; set; }
            }

            public class Location
            {
                public double lat { get; set; }
                public double lon { get; set; }
                public string name { get; set; }
            }

            public class Param
            {
                public string type { get; set; }
                public string name { get; set; }
                public string value { get; set; }
            }

            public class Payment
            {
                public DateTime due { get; set; }
                public double amount { get; set; }
            }

            public class Rate
            {
                public int id { get; set; }
                public string type { get; set; }
                public int board { get; set; }
                public string url { get; set; }
                public string room { get; set; }
                public string roomurl { get; set; }
                public int remaining { get; set; }
                public string rate { get; set; }
                public string rate_desc { get; set; }
                public string photo { get; set; }
                public string photoM { get; set; }
                public string photoL { get; set; }
                public double stay { get; set; }
                public double extras { get; set; }
                public double taxes { get; set; }
                public double excluded_charges { get; set; }
                public double price { get; set; }
                public double retail { get; set; }
                public double discount { get; set; }
                public double margin { get; set; }
                public string payment_policy { get; set; }
                public int payment_policy_id { get; set; }
                public string cancellation_policy { get; set; }
                public int cancellation_policy_id { get; set; }
                public string cancellation_penalty { get; set; }
                public string cancellation_expiry { get; set; } //change object string bcoz error throw in System.Json when empty string //030425
                public List<Payment> payments { get; set; }
                public List<CancellationFee> cancellation_fees { get; set; }
            }

            public class Root
            {
                public string request_id { get; set; }
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<Param> @params { get; set; }
                public Data data { get; set; }
            }



        }

        public class WebHotelierBookResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Data
            {
                public string summaryUrl { get; set; }
                public int res_id { get; set; }
                public string email { get; set; }
                public string result { get; set; }
            }

            public class Root
            {
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<object> @params { get; set; }
                public Data data { get; set; }
            }



        }

        public class WebhotelierCancelResponse
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Data
            {
                public string result { get; set; }
                public double cancellation_penalty_amount { get; set; }
                public string cancellation_penalty_currency { get; set; }
            }

            public class Root
            {
                public string method { get; set; }
                public string http_method { get; set; }
                public int http_code { get; set; }
                public string error_code { get; set; }
                public string error_msg { get; set; }
                public List<object> @params { get; set; }
                public Data data { get; set; }
            }


        }
    }

    public class webHotelliearHotelDetails
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Children
        {
            public int allowed { get; set; }
            public int age_from { get; set; }
            public int age_to { get; set; }
            public string policy { get; set; }
        }

        public class Contact
        {
            public string tel { get; set; }
            public string fax { get; set; }
            public string email { get; set; }
            public string skype { get; set; }
        }

        public class Data
        {
            public string code { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int rating { get; set; }
            public string currency { get; set; }
            public string description { get; set; }
            public string general_terms { get; set; }
            public string directions { get; set; }
            public string license_number { get; set; }
            public string url { get; set; }
            public string bookurl { get; set; }
            public Contact contact { get; set; }
            public Location location { get; set; }
            public Children children { get; set; }
            public Settings settings { get; set; }
            public Operation operation { get; set; }
            public List<Room> rooms { get; set; }
            public List<string> facilities { get; set; }
            public List<Photo> photos { get; set; }
            public string logourl { get; set; }
        }

        public class Location
        {
            public double lat { get; set; }
            public double lon { get; set; }
            public int utc_offset { get; set; }
            public string timezone { get; set; }
            public string name { get; set; }
            public string address { get; set; }
            public string zip { get; set; }
            public string country { get; set; }
        }

        public class Operation
        {
            public string checkout_time { get; set; }
            public string checkin_time { get; set; }
            public string open_from { get; set; }
            public string open_to { get; set; }
        }

        public class Photo
        {
            public string title { get; set; }
            public string xsmall { get; set; }
            public string small { get; set; }
            public string medium { get; set; }
            public string large { get; set; }
        }

        public class Room
        {
            public string code { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Root
        {
            public string request_id { get; set; }
            public string method { get; set; }
            public string http_method { get; set; }
            public int http_code { get; set; }
            public string error_code { get; set; }
            public string error_msg { get; set; }
            public List<object> @params { get; set; }
            public Data data { get; set; }
        }

        public class Settings
        {
            public int nights_min { get; set; }
            public int nights_max { get; set; }
            public int rooms_max { get; set; }
        }


    }
}
