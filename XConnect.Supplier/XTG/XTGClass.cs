
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using MongoDB.Driver.Core.Misc;
using Newtonsoft.Json.Linq;
using static Azure.Core.HttpHeader;
using System.Net;
using System.IO.Compression;
using Azure;


namespace XConnect.Supplier.XTG
{
    public class XTGClass
    {
    }


    public class Pax
    {
        public int age { get; set; }
    }

    public class Occupancy
    {
        public List<Pax> paxes { get; set; }
    }

    public class CriteriaSearch
    {
        public string checkIn { get; set; }
        public string checkOut { get; set; }
        public List<string> hotels { get; set; }
        public List<Occupancy> occupancies { get; set; }
        public string language { get; set; }
        public string nationality { get; set; }
        public string currency { get; set; }
        public string market { get; set; }
    }
    public class BusinessRules
    {
        // public int optionsQuota { get; set; }
        public string businessRulesType { get; set; }
    }

    public class Settings
    {
        public string client { get; set; }
        public int timeout { get; set; }
        public BusinessRules businessRules { get; set; }
        public bool auditTransactions { get; set; }
        public string context { get; set; }

        public bool testMode { get; set; }

    }
    public class XMLBaseID_search
    {
        public string XMLBaseID { get; set; }
    }
    public class XMLID_search
    {
        public string XMLID { get; set; }
    }

    public class Access
    {
        public List<string> includes { get; set; }
    }

    public class Filter
    {
        public Access access { get; set; }
    }
    public class XTGSearchRQ
    {
        public CriteriaSearch criteriaSearch { get; set; }
        public Settings settings { get; set; }
        public Filter filter { get; set; }
    }
    public class GraphQLQuery_Response
    {
        public string Response { get; set; }
        public bool IsError { get; set; }
    }
    public class GraphQLClient
    {
        public class GraphQLQuery
        {
            // public string OperationName { get; set; }
            public string query { get; set; }
            public object variables { get; set; }
        }

        public class GraphQLQueryResult
        {
            private string raw;
            //private JObject data;
            private JsonDocument data;
            private Exception Exception;
            public GraphQLQueryResult(string text, Exception ex = null)
            {
                Exception = ex;
                raw = text;
                data = text != null ? JsonDocument.Parse(text) : null;
            }



            public Exception GetException()
            {
                return Exception;
            }
            public string GetRaw()
            {
                return raw;
            }
            //public T Get<T>(string key)
            //{
            //    if (data == null) return default;
            //    try
            //    {
            //        return JsonConvert.DeserializeObject<T>(data["data"][key].ToString());
            //        //return JsonSerializer.Deserialize<T>(data["data"][key].ToString());



            //    }
            //    catch
            //    {
            //        return default;
            //    }
            //}
            //public dynamic Get(string key)
            //{
            //    if (data == null) return null;
            //    try
            //    {
            //        return JsonConvert.DeserializeObject<dynamic>(data["data"][key].ToString());
            //    }
            //    catch
            //    {
            //        return null;
            //    }
            //}
            //public dynamic GetData()
            //{
            //    if (data == null) return null;
            //    try
            //    {
            //        return JsonConvert.DeserializeObject<dynamic>(data["data"].ToString());
            //    }
            //    catch
            //    {
            //        return null;
            //    }
            //}
        }
        private string url;
        public GraphQLClient(string url)
        {
            this.url = url;
        }

        #region "List Query "
        public static string query = @"
    query ($criteriaSearch: HotelCriteriaSearchInput, $settings: HotelSettingsInput, $filter: FilterInput) {
  hotelX {
    search(criteria: $criteriaSearch, settings: $settings, filter: $filter) {
      context
      options {
        hotelCode
        hotelName
        accessCode
        supplierCode
        market
        status
        rooms {
          units
          roomPrice {
            price {
              currency
              binding
              net
              gross
            }
          }
          promotions {
            code
            name
            effectiveDate
            expireDate
          }
        }
      }
  auditData{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
        
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}


";
        #endregion
        #region"Search With All Query"
        public static string queryfull = @"
   # Example Search - 1 rooms 2 pax
query ($criteriaSearch: HotelCriteriaSearchInput, $settings: HotelSettingsInput, $filter: FilterInput) {
  hotelX {
    search(criteria: $criteriaSearch, settings: $settings, filter: $filter) {
      context
      options {
        surcharges {
          chargeType
          mandatory
          description
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
        }
        accessCode
        supplierCode
        market
        hotelCode
        hotelName
        boardCode
        paymentType
        status
        occupancies {
          id
          paxes {
            age
          }
        }
        rooms {
          occupancyRefId
          code
          description
          refundable
          units
          roomPrice {
            price {
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
              markups {
                channel
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
              }
            }
          }
          beds {
            type
            description
            count
            shared
          }
          ratePlans {
            code
            name
            effectiveDate
            expireDate
          }
          promotions {
            code
            name
            effectiveDate
            expireDate
          }
        }
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        addOns {
          distribute
        }
        supplements {
          code
          name
          description
          supplementType
          chargeType
          mandatory
          durationType
          quantity
          unit
          effectiveDate
          expireDate
          resort {
            code
            name
            description
          }
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
        }
        surcharges {
          chargeType
          description
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
        }
        rateRules
        cancelPolicy {
          refundable
          cancelPenalties {
            hoursBefore
            penaltyType
            currency
            value
          }
        }
        remarks
        token
        id
      }
  auditData{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
        
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
        #endregion
        #region "Quote Query"
        public static string XTG_Quote_Query = @"query ($criteriaQuote: HotelCriteriaQuoteInput!,$settings: HotelSettingsInput) {
  hotelX {
    quote(criteria: $criteriaQuote, settings: $settings) {
      optionQuote {
        optionRefId
        status
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        surcharges {
          chargeType
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
          description
        }
        cancelPolicy {
          refundable
          cancelPenalties {
            hoursBefore
            penaltyType
            currency
            value
          }
        }
        cardTypes
        remarks
      }
 auditData	{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
        #endregion
        #region "Book Query"
        public static string XTG_BOOK_Query = @"mutation ($BookInput: HotelBookInput!, $settings: HotelSettingsInput) {
  hotelX {
    book(input: $BookInput, settings: $settings) {
      booking {
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        status
        remarks
        reference {
          client
          supplier
        }
        holder {
          name
          surname
        }
        hotel {
          creationDate
          checkIn
          checkOut
          hotelCode
          hotelName
          boardCode
          occupancies {
            id
            paxes {
              age
            }
          }
          rooms {
            code
            description
            occupancyRefId
            price {
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
              markups {
                channel
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
              }
            }
          }
        }
      }
auditData	{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
        #endregion

        #region "Read Query"
        public static string XTG_ReadQuery = @"query ($criteriaBookingRead: HotelCriteriaBookingInput!, $settings: HotelSettingsInput) {
  hotelX {
    booking(criteria: $criteriaBookingRead, settings: $settings) {
      bookings {
        reference {
          client
          supplier
        }
        holder {
          name
          surname
        }
        hotel {
          creationDate
          checkIn
          checkOut
          hotelCode
          hotelName
          boardCode
          occupancies {
            id
            paxes {
              age
            }
          }
          rooms {
            occupancyRefId
            code
            description
            price {
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
              markups {
                channel
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
              }
            }
          }
        }
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        cancelPolicy {
          refundable
          cancelPenalties {
            hoursBefore
            penaltyType
            currency
            value
          }
        }
        remarks
        status
        payable
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
        #endregion

        #region "Cancel Qurey"
        public static string XTG_CancelQuery = @"mutation ($CancelInput: HotelCancelInput!, $settings: HotelSettingsInput) {
  hotelX {
    cancel(input: $CancelInput, settings: $settings) {
      cancellation {
        reference {
          client
          supplier
        }
        cancelReference
        status
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        booking {
          reference {
            client
            supplier
          }
          holder {
            name
            surname
          }
          hotel {
            creationDate
            checkIn
            checkOut
            hotelCode
            hotelName
            boardCode
            occupancies {
              id
              paxes {
                age
              }
            }
            rooms {
              occupancyRefId
              code
              description
              price {
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
                markups {
                  channel
                  currency
                  binding
                  net
                  gross
                  exchange {
                    currency
                    rate
                  }
                }
              }
            }
          }
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
          cancelPolicy {
            refundable
            cancelPenalties {
              hoursBefore
              penaltyType
              currency
              value
            }
          }

          remarks
          status
          payable
        }
      }
 auditData	{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
      }
    }
  }
}
";
        #endregion

    }
    #region Hotellist
    public class Xtg_hotellist
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class AuditData
        {
            public List<Transaction> transactions { get; set; }
            public string timeStamp { get; set; }
            public int processTime { get; set; }
        }

        public class Data
        {
            public HotelX hotelX { get; set; }
        }

        public class HotelX
        {
            public Search search { get; set; }
        }

        public class Option
        {
            public string hotelCode { get; set; }
            public object hotelName { get; set; }
            public string accessCode { get; set; }
            public string supplierCode { get; set; }
            public string market { get; set; }
            public string status { get; set; }
            public List<Room> rooms { get; set; }
        }

        public class Price
        {
            public string currency { get; set; }
            public bool binding { get; set; }
            public double net { get; set; }
            public double gross { get; set; }
        }

        public class Room
        {
            public object units { get; set; }
            public RoomPrice roomPrice { get; set; }
            public object promotions { get; set; }
        }

        public class RoomPrice
        {
            public Price price { get; set; }
        }

        public class Root
        {
            public Data data { get; set; }
        }

        public class Search
        {
            public string context { get; set; }
            public List<Option> options { get; set; }
            public AuditData auditData { get; set; }
            public object errors { get; set; }
            public object warnings { get; set; }
        }

        public class Transaction
        {
            public string request { get; set; }
            public string response { get; set; }
        }


    }
    #endregion

    #region XTG_Roomlist
    public class Xtg_Roomlist
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class AddOns
        {
            public object distribute { get; set; }
        }

        public class AuditData
        {
            public List<Transaction> transactions { get; set; }
            public string timeStamp { get; set; }
            public int processTime { get; set; }
        }

        public class CancelPenalty
        {
            public int hoursBefore { get; set; }
            public string penaltyType { get; set; }
            public string currency { get; set; }
            public double value { get; set; }
        }

        public class CancelPolicy
        {
            public bool refundable { get; set; }
            public List<CancelPenalty> cancelPenalties { get; set; }
        }

        public class Data
        {
            public HotelX hotelX { get; set; }
        }

        public class Exchange
        {
            public string currency { get; set; }
            public int rate { get; set; }
        }

        public class HotelX
        {
            public Search search { get; set; }
        }

        public class Occupancy
        {
            public int id { get; set; }
            public List<Paxis> paxes { get; set; }
        }

        public class Option
        {
            public object surcharges { get; set; }
            public string accessCode { get; set; }
            public string supplierCode { get; set; }
            public string market { get; set; }
            public string hotelCode { get; set; }
            public object hotelName { get; set; }
            public string boardCode { get; set; }
            public string paymentType { get; set; }
            public string status { get; set; }
            public List<Occupancy> occupancies { get; set; }
            public List<Room> rooms { get; set; }
            public Price price { get; set; }
            public AddOns addOns { get; set; }
            public supplements supplements { get; set; }
            public List<string> rateRules { get; set; }
            public CancelPolicy cancelPolicy { get; set; }
            public object remarks { get; set; }
            public string token { get; set; }
            public string id { get; set; }
        }

        public class Paxis
        {
            public int age { get; set; }
        }
        public class supplements
        {
            public string code { get; set; }
            public string supplementType { get; set; }
            public string chargeType { get; set; }
            public string mandatory { get; set; }
            public string quantity { get; set; }
            public string unit { get; set; }
            public string effectiveDate { get; set; }
            public string expireDate { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string durationType { get; set; }
            public resort resort { get; set; }

            // public string Id { get; set; }

        }
        public class resort
        {
            public string code { get; set; }
            public string name { get; set; }
            public string description { get; set; }
        }
        public class Price
        {
            public string currency { get; set; }
            public bool binding { get; set; }
            public double net { get; set; }
            public double gross { get; set; }
            public Exchange exchange { get; set; }
            public object markups { get; set; }
        }

        public class Room
        {
            public int occupancyRefId { get; set; }
            public string code { get; set; }
            public string description { get; set; }
            public features? features { get; set; } //add nubale in 08/12/25
            public bool? refundable { get; set; }
            public object units { get; set; }
            public RoomPrice roomPrice { get; set; }
            public List<beds>? beds { get; set; } //add nubale in 08/12/25
            //public ratePlans ratePlans { get; set; }//comment on 08/12/25
            public List<ratePlans>? ratePlans { get; set; } //add nubale and List in 08/12/25
            public List<promotions>?  promotions { get; set; }  //add nubale in 08/12/25
        }

        public class RoomPrice
        {
            public Price price { get; set; }
        }
        public class features
        {
            public string code { get; set; }
        }
        public class beds
        {
            public string type { get; set; }
            public int count { get; set; }
            public string description { get; set; }

            public string shared { get; set; }
        }
        public class ratePlans
        {
            public string code { get; set; }
            public string effectiveDate { get; set; }
            public string expireDate { get; set; }
        }
        public class promotions
        {
            public string code { get; set; }
            public string name { get; set; }
            public string effectiveDate { get; set; }
            public string expireDate { get; set; }

        }

        public class Root
        {
            public Data data { get; set; }
        }

        public class Search
        {
            public string context { get; set; }
            public List<Option> options { get; set; }
            public AuditData auditData { get; set; }
            public object errors { get; set; }
            public object warnings { get; set; }
        }

        public class Transaction
        {
            public string request { get; set; }
            public string response { get; set; }
        }


    }
    #endregion
    public class Room_NameMerge
    {
        public int RoomCount { get; set; }
        public string RoomName { get; set; }
        public bool status { get; set; }
    }


    public class Price_Xtg
    {
        public string currency { get; set; }
        public bool binding { get; set; }
        public double net { get; set; }
        public double gross { get; set; }
    }

    public class RoomPrice_Xtg
    {
        public Price_Xtg price { get; set; }
    }

    public class Room_Xtg
    {
        public RoomPrice_Xtg roomPrice { get; set; }
    }

    public class Option_Xtg
    {
        public string hotelCode { get; set; }
        public string hotelName { get; set; }
        public string accessCode { get; set; }
        public string supplierCode { get; set; }
        public string market { get; set; }
        public string status { get; set; }
        public List<Room_Xtg> rooms { get; set; }
    }
    public class Transaction_Xtg
    {
        public string request { get; set; }
        public string response { get; set; }
    }

    public class AuditData_Xtg
    {
        public List<Transaction_Xtg> transactions { get; set; }
        public string timeStamp { get; set; }
        public int processTime { get; set; }
    }

    public class Error_Xtg
    {
        public string code { get; set; }
        public string type { get; set; }
        public string description { get; set; }
    }

    public class Warning_Xtg
    {
        public string code { get; set; }
        public string type { get; set; }
        public string description { get; set; }
    }

    public class Search_Xtg
    {
        public string context { get; set; }
        public List<Option_Xtg> options { get; set; }
        public AuditData_Xtg auditData { get; set; }
        public List<Error_Xtg> errors { get; set; }
        public List<Warning_Xtg> warnings { get; set; }
    }

    public class HotelX_Xtg
    {
        public Search_Xtg search { get; set; }
    }

    public class Data_Xtg
    {
        public HotelX_Xtg hotelX { get; set; }
    }

    public class XTG_SearchResponse
    {
        public Data_Xtg data { get; set; }
    }


    public class CriteriaQuote
    {
        public string optionRefId { get; set; }
    }

    public class QuoteRequest
    {
        public CriteriaQuote criteriaQuote { get; set; }
        public Settings settings { get; set; }
    }



    public class DeltaPrice
    {
        public int amount { get; set; }
        public int percent { get; set; }
        public bool applyBoth { get; set; }
    }

    public class Holder
    {
        public string name { get; set; }
        public string surname { get; set; }
        public string title { get; set; }

    }

    public class Paxbook
    {
        public string name { get; set; }
        public string surname { get; set; }
        public string title { get; set; }
        public int age { get; set; }

    }

    public class Room
    {
        public int occupancyRefId { get; set; }
        public List<Paxbook> paxes { get; set; }
    }

    public class BookInput
    {
        public string optionRefId { get; set; }
        public string clientReference { get; set; }
        public DeltaPrice deltaPrice { get; set; }
        public Holder holder { get; set; }
        public List<Room> rooms { get; set; }
    }

    public class XTG_BookRQ
    {
        public BookInput BookInput { get; set; }
        public Settings settings { get; set; }
    }
    public class Reference
    {
        public string client { get; set; }
        public string supplier { get; set; }
    }

    public class CancelInput
    {
        public string accessCode { get; set; }
        public string language { get; set; }
        public string hotelCode { get; set; }
        public Reference reference { get; set; }
    }

    public class XTG_CancelRQ
    {
        public CancelInput CancelInput { get; set; }
        public Settings settings { get; set; }
    }

    public class SettingsRead
    {
        public string client { get; set; }
        public string context { get; set; }
        public bool testMode { get; set; }
        public bool auditTransactions { get; set; }
    }
    public class References
    {
        public string hotelCode { get; set; }
        public string currency { get; set; }
        public List<Reference> references { get; set; }
    }

    public class CriteriaBookingRead
    {
        public string accessCode { get; set; }
        public string language { get; set; }
        public string typeSearch { get; set; }
        public References references { get; set; }
    }

    public class XTG_ReadRQ
    {
        public CriteriaBookingRead criteriaBookingRead { get; set; }

        public SettingsRead settings { get; set; }

    }

    //recheck
    public class XMLTravelGate
    {
        public XMLTravelGate()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        public static string XTG_Language = "en";
        public static string XTG_Currency = "USD";
        public class Pax
        {
            public int age { get; set; }
        }

        public class Occupancy
        {
            public List<Pax> paxes { get; set; }
        }

        public class CriteriaSearch
        {
            public string checkIn { get; set; }
            public string checkOut { get; set; }
            public List<string> hotels { get; set; }
            public List<Occupancy> occupancies { get; set; }
            public string language { get; set; }
            public string nationality { get; set; }
            public string currency { get; set; }
            public string market { get; set; }
        }
        public class BusinessRules
        {
            // public int optionsQuota { get; set; }
            public string businessRulesType { get; set; }
        }

        public class Settings
        {
            public string client { get; set; }
            public int timeout { get; set; }
            public BusinessRules businessRules { get; set; }
            public bool auditTransactions { get; set; }
            public string context { get; set; }

            public bool testMode { get; set; }

        }


        public class Access
        {
            public List<string> includes { get; set; }
        }

        public class Filter
        {
            public Access access { get; set; }
        }
        public class XTGSearchRQ
        {
            public CriteriaSearch criteriaSearch { get; set; }
            public Settings settings { get; set; }
            public Filter filter { get; set; }
        }
        public class GraphQLQuery_Response
        {
            public string Response { get; set; }
            public bool IsError { get; set; }
        }

        public class GQDto //added by jay on 200625
        {
            public GraphQLQuery_Response GQRes { get; set; }
            public string Error { get; set; }
        }
        public class GraphQLClient
        {
            public class GraphQLQuery
            {
                // public string OperationName { get; set; }
                public string query { get; set; }
                public object variables { get; set; }
            }

            public class GraphQLQueryResult
            {
                private string raw;
                private JObject data;
                private Exception Exception;
                public GraphQLQueryResult(string text, Exception ex = null)
                {
                    Exception = ex;
                    raw = text;
                    data = text != null ? JObject.Parse(text) : null;
                }


                public Exception GetException()
                {
                    return Exception;
                }
                public string GetRaw()
                {
                    return raw;
                }
                public T Get<T>(string key)
                {
                    if (data == null) return default(T);
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(this.data["data"][key].ToString());
                    }
                    catch
                    {
                        return default(T);
                    }
                }
                public dynamic Get(string key)
                {
                    if (data == null) return null;
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(this.data["data"][key].ToString());
                    }
                    catch
                    {
                        return null;
                    }
                }
                public dynamic GetData()
                {
                    if (data == null) return null;
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(this.data["data"].ToString());
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            private string url;
            public GraphQLClient(string url)
            {
                this.url = url;
            }
            #region "GetXTG Response"
            public GraphQLQuery_Response GetXTGResponse(ref string Error, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
            {
                GraphQLQuery_Response final = new GraphQLQuery_Response();
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();


                try
                {
                    var fullQuery = new GraphQLQuery()
                    {
                        query = query,
                        variables = variables,
                    };
                    string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(fullQuery);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    UTF8Encoding utf8Encoding = new UTF8Encoding();
                    Byte[] byteArray = utf8Encoding.GetBytes(jsonContent.Trim());
                    ServicePointManager.DefaultConnectionLimit = 1000;
                    request.ContentLength = byteArray.Length;
                    request.ContentType = @"application/json";
                    request.Headers.Add("Accept-Encoding", "gzip");
                    request.Timeout = TimeOut;
                    request.Headers.Add("Authorization", "Apikey " + Apikey + "");
                    using (Stream dataStream = request.GetRequestStream())
                    {
                        dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                    }
                    try
                    {
                       // HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                        HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                        Stream responseStream = null;
                        if (Response.Headers["Content-Encoding"]?.ToLower().Contains("gzip") == true)
                        {
                            responseStream = new GZipStream(Response.GetResponseStream(), CompressionMode.Decompress);
                        }
                        else
                        responseStream = Response.GetResponseStream();

                        //Read the output
                        StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                        string strOutput = Reader.ReadToEnd();
                        responseStream.Close();
                        Reader.Close();
                        Response.Close();
                        final.Response = strOutput;
                        final.IsError = false;
                        timer.Stop();
                        if (para != null)
                        {
                            para.SupplierTime += ",SupplierName :" + Provider + ": " + "Execution Time :" + timer.Elapsed.ToString();
                            //if (DebugMode)
                                //Common.XMLRequestResponseTrack(Convert.ToString(variables), strOutput, Provider, "Proper");
                        }
                        //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        //{
                        //    length = response.ContentLength;
                        //    using (Stream responseStream = response.GetResponseStream())
                        //    {
                        //        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        //        string json = reader.ReadToEnd();
                        //        return json;
                        //        // return new GraphQLQueryResult(json);
                        //    }
                        //}
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response != null)
                        {
                            WebResponse errorResponse = ex.Response;
                            using (Stream responseStream = errorResponse.GetResponseStream())
                            {
                                StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                                String errorText = reader.ReadToEnd();
                                //  Console.WriteLine(errorText);
                                final.Response = errorText;
                                final.IsError = true;
                                // return new GraphQLQueryResult(null, ex);
                            }
                        }
                        else
                        {
                            final.Response = "timeout";
                            final.IsError = true;
                        }
                        Error = final.Response + "#Message= " + ex.Message;
                    }
                }
                catch (Exception ex)
                {

                    //Common.Error(ex);
                    final.Response = ex.Message;
                    final.IsError = true;
                }

                return final;
            }

           

            public async Task<GQDto> GetXTGResponseAsync(string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
            {
                GQDto res = new GQDto();
                string Error = "";

                GraphQLQuery_Response final = new GraphQLQuery_Response();
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();


                try
                {
                    var fullQuery = new GraphQLQuery()
                    {
                        query = query,
                        variables = variables,
                    };
                    string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(fullQuery);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    UTF8Encoding utf8Encoding = new UTF8Encoding();
                    Byte[] byteArray = utf8Encoding.GetBytes(jsonContent.Trim());
                    ServicePointManager.DefaultConnectionLimit = 1000;
                    request.ContentLength = byteArray.Length;
                    request.ContentType = @"application/json";
                    request.Headers.Add("Accept-Encoding", "gzip");
                    request.Timeout = TimeOut;
                    request.Headers.Add("Authorization", "Apikey " + Apikey + "");
                    using (Stream dataStream =  await  request.GetRequestStreamAsync())
                    {
                        dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                    }
                    try
                    {
                        // HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                        HttpWebResponse Response = (HttpWebResponse)await request.GetResponseAsync();
                        Stream responseStream = null;

                        var encoding = Response.Headers["Content-Encoding"]?.ToLower();

                        if (encoding?.Contains("gzip") == true)
                        {
                            responseStream = new GZipStream(Response.GetResponseStream(), CompressionMode.Decompress);
                        }
                        else if (encoding?.Contains("deflate") == true)
                        {
                            responseStream = new DeflateStream(Response.GetResponseStream(), CompressionMode.Decompress);
                        }
                        else
                        {
                            responseStream = Response.GetResponseStream();
                        }
                        //Read the output
                        StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                        string strOutput = await Reader.ReadToEndAsync();
                        responseStream.Close();
                        Reader.Close();
                        Response.Close();
                        final.Response = strOutput;
                        final.IsError = false;
                        timer.Stop();
                        if (para != null)
                        {
                            para.SupplierTime += ",SupplierName :" + Provider + ": " + "Execution Time :" + timer.Elapsed.ToString();
                            //if (DebugMode)
                            //Common.XMLRequestResponseTrack(Convert.ToString(variables), strOutput, Provider, "Proper");
                        }
                        
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response != null)
                        {
                            WebResponse errorResponse = ex.Response;
                            using (Stream responseStream = errorResponse.GetResponseStream())
                            {
                                StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                                String errorText = await reader.ReadToEndAsync();
                                //  Console.WriteLine(errorText);
                                final.Response = errorText;
                                final.IsError = true;
                                // return new GraphQLQueryResult(null, ex);
                            }
                        }
                        else
                        {
                            final.Response = "timeout";
                            final.IsError = true;
                        }
                        Error = final.Response + "#Message= " + ex.Message;
                    }
                }
                catch (Exception ex)
                {

                    //Common.Error(ex);
                    final.Response = ex.Message;
                    final.IsError = true;
                }

                res.GQRes = final;
                res.Error = Error;

                return res;
            }


            #endregion
            #region "List Query "
            public static string query = @"
    query ($criteriaSearch: HotelCriteriaSearchInput, $settings: HotelSettingsInput, $filter: FilterInput) {
  hotelX {
    search(criteria: $criteriaSearch, settings: $settings, filter: $filter) {
      context
      options {
        hotelCode
        hotelName
        accessCode
        supplierCode
        market
        status
        rooms {
          units
          roomPrice {
            price {
              currency
              binding
              net
              gross
            }
          }
          promotions {
            code
            name
            effectiveDate
            expireDate
          }
        }
      }
  auditData{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
        
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}


";
            #endregion
            #region"Search With All Query"
            public static string queryfull = @"
   # Example Search - 1 rooms 2 pax
query ($criteriaSearch: HotelCriteriaSearchInput, $settings: HotelSettingsInput, $filter: FilterInput) {
  hotelX {
    search(criteria: $criteriaSearch, settings: $settings, filter: $filter) {
      context
      options {
        surcharges {
          chargeType
          mandatory
          description
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
        }
        accessCode
        supplierCode
        market
        hotelCode
        hotelName
        boardCode
        paymentType
        status
        occupancies {
          id
          paxes {
            age
          }
        }
        rooms {
          occupancyRefId
          code
          description
          refundable
          units
          roomPrice {
            price {
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
              markups {
                channel
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
              }
            }
          }
          beds {
            type
            description
            count
            shared
          }
          ratePlans {
            code
            name
            effectiveDate
            expireDate
          }
          promotions {
            code
            name
            effectiveDate
            expireDate
          }
        }
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        addOns {
          distribute
        }
        supplements {
          code
          name
          description
          supplementType
          chargeType
          mandatory
          durationType
          quantity
          unit
          effectiveDate
          expireDate
          resort {
            code
            name
            description
          }
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
        }
        surcharges {
          chargeType
          description
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
        }
        rateRules
        cancelPolicy {
          refundable
          cancelPenalties {
            hoursBefore
            penaltyType
            currency
            value
          }
        }
        remarks
        token
        id
      }
  auditData{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
        
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
            #endregion
            #region "Quote Query"
            public static string XTG_Quote_Query = @"query ($criteriaQuote: HotelCriteriaQuoteInput!,$settings: HotelSettingsInput) {
  hotelX {
    quote(criteria: $criteriaQuote, settings: $settings) {
      optionQuote {
        optionRefId
        status
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        surcharges {
          chargeType
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
          description
        }
        cancelPolicy {
          refundable
          cancelPenalties {
            hoursBefore
            penaltyType
            currency
            value
          }
        }
        cardTypes
        remarks
      }
 auditData	{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
            #endregion
            #region "Book Query"
            public static string XTG_BOOK_Query = @"mutation ($BookInput: HotelBookInput!, $settings: HotelSettingsInput) {
  hotelX {
    book(input: $BookInput, settings: $settings) {
      booking {
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        status
        remarks
        reference {
          client
          supplier
        }
        holder {
          name
          surname
        }
        hotel {
          creationDate
          checkIn
          checkOut
          hotelCode
          hotelName
          boardCode
          occupancies {
            id
            paxes {
              age
            }
          }
          rooms {
            code
            description
            occupancyRefId
            price {
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
              markups {
                channel
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
              }
            }
          }
        }
      }
auditData	{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
            #endregion

            #region "Read Query"
            public static string XTG_ReadQuery = @"query ($criteriaBookingRead: HotelCriteriaBookingInput!, $settings: HotelSettingsInput) {
  hotelX {
    booking(criteria: $criteriaBookingRead, settings: $settings) {
      bookings {
        reference {
          client
          supplier
        }
        holder {
          name
          surname
        }
        hotel {
          creationDate
          checkIn
          checkOut
          hotelCode
          hotelName
          boardCode
          occupancies {
            id
            paxes {
              age
            }
          }
          rooms {
            occupancyRefId
            code
            description
            price {
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
              markups {
                channel
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
              }
            }
          }
        }
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        cancelPolicy {
          refundable
          cancelPenalties {
            hoursBefore
            penaltyType
            currency
            value
          }
        }
        remarks
        status
        payable
      }
      errors {
        code
        type
        description
      }
      warnings {
        code
        type
        description
      }
    }
  }
}
";
            #endregion

            #region "Cancel Qurey"
            public static string XTG_CancelQuery = @"mutation ($CancelInput: HotelCancelInput!, $settings: HotelSettingsInput) {
  hotelX {
    cancel(input: $CancelInput, settings: $settings) {
      cancellation {
        reference {
          client
          supplier
        }
        cancelReference
        status
        price {
          currency
          binding
          net
          gross
          exchange {
            currency
            rate
          }
          markups {
            channel
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
          }
        }
        booking {
          reference {
            client
            supplier
          }
          holder {
            name
            surname
          }
          hotel {
            creationDate
            checkIn
            checkOut
            hotelCode
            hotelName
            boardCode
            occupancies {
              id
              paxes {
                age
              }
            }
            rooms {
              occupancyRefId
              code
              description
              price {
                currency
                binding
                net
                gross
                exchange {
                  currency
                  rate
                }
                markups {
                  channel
                  currency
                  binding
                  net
                  gross
                  exchange {
                    currency
                    rate
                  }
                }
              }
            }
          }
          price {
            currency
            binding
            net
            gross
            exchange {
              currency
              rate
            }
            markups {
              channel
              currency
              binding
              net
              gross
              exchange {
                currency
                rate
              }
            }
          }
          cancelPolicy {
            refundable
            cancelPenalties {
              hoursBefore
              penaltyType
              currency
              value
            }
          }

          remarks
          status
          payable
        }
      }
 auditData	{
        transactions{
          request
          response
      
        }
        timeStamp
        processTime
      }
    }
  }
}
";
            #endregion

        }
        public static Int32 AdultAge = 35;
        #region "Quote"
        public class CriteriaQuote
        {
            public string optionRefId { get; set; }
        }

        public class QuoteRequest
        {
            public CriteriaQuote criteriaQuote { get; set; }
            public Settings settings { get; set; }
        }
        #endregion

        #region"Book"
        public class DeltaPrice
        {
            public int amount { get; set; }
            public int percent { get; set; }
            public bool applyBoth { get; set; }
        }

        public class Holder
        {
            public string name { get; set; }
            public string surname { get; set; }
            public string title { get; set; }

        }

        public class Paxbook
        {
            public string name { get; set; }
            public string surname { get; set; }
            public string title { get; set; }
            public int age { get; set; }

        }

        public class Room
        {
            public int occupancyRefId { get; set; }
            public List<Paxbook> paxes { get; set; }
        }

        public class BookInput
        {
            public string optionRefId { get; set; }
            public string clientReference { get; set; }
            public DeltaPrice deltaPrice { get; set; }
            public Holder holder { get; set; }
            public List<Room> rooms { get; set; }
        }

        public class XTG_BookRQ
        {
            public BookInput BookInput { get; set; }
            public Settings settings { get; set; }
        }
        #endregion

        #region "Cancel"
        public class Reference
        {
            public string client { get; set; }
            public string supplier { get; set; }
        }

        public class CancelInput
        {
            public string accessCode { get; set; }
            public string language { get; set; }
            public string hotelCode { get; set; }
            public Reference reference { get; set; }
        }

        public class XTG_CancelRQ
        {
            public CancelInput CancelInput { get; set; }
            public Settings settings { get; set; }
        }
        #endregion

        #region "Read"


        public class References
        {
            public string hotelCode { get; set; }
            public string currency { get; set; }
            public List<Reference> references { get; set; }
        }

        public class CriteriaBookingRead
        {
            public string accessCode { get; set; }
            public string language { get; set; }
            public string typeSearch { get; set; }
            public References references { get; set; }
        }

        public class XTG_ReadRQ
        {
            public CriteriaBookingRead criteriaBookingRead { get; set; }
        }
        #endregion
    }

    [Serializable]
    public class ExchangeBYID
    {
        public string currency { get; set; }
        public int rate { get; set; }
    }

    public class PriceBYID
    {
        public string currency { get; set; }
        public bool binding { get; set; }
        public double? net { get; set; }
        public double? gross { get; set; }
        public ExchangeBYID exchange { get; set; }
        public object markups { get; set; }
    }

    public class ReferenceBYID
    {
        public string client { get; set; }
        public string supplier { get; set; }
    }
    [Serializable]
    public class RoomBYID
    {
        public int occupancyRefId { get; set; }
        public string code { get; set; }
        public object description { get; set; }
        public object price { get; set; }
    }
    [Serializable]
    public class HotelBYID
    {
        public string creationDate { get; set; }
        public string checkIn { get; set; }
        public string checkOut { get; set; }
        public string hotelCode { get; set; }
        public string hotelName { get; set; }
        public string boardCode { get; set; }
        public object occupancies { get; set; }
        public List<RoomBYID> rooms { get; set; }
    }
    public class Booking2BYID
    {
        public ReferenceBYID reference { get; set; }
        public object holder { get; set; }
        public HotelBYID hotel { get; set; }
        public PriceBYID price { get; set; }
        public object cancelPolicy { get; set; }
        public object remarks { get; set; }
        public string status { get; set; }
        public object payable { get; set; }
    }

    [Serializable]
    public class BookingBYID
    {
        public List<Booking2BYID> bookings { get; set; }
        public object errors { get; set; }
        public object warnings { get; set; }
    }

    [Serializable]
    public class HotelXBYID
    {
        public BookingBYID booking { get; set; }
    }

    [Serializable]
    public class DataBYID
    {
        public HotelXBYID hotelX { get; set; }
    }

    [Serializable]
    public class BookingByInternalID
    {
        public DataBYID data { get; set; }
    }
}



