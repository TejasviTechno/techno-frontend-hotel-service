using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Hoteltrader
{
    public class HotelTraderClass
    {
    }

    #region HotelTraderClass


    public static class HotelTraderSearchRequet
    {
        public class Occupancy
        {
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public int numberOfAdults { get; set; }
            public int numberOfChildren { get; set; }
            public string childrenAges { get; set; }
        }

        public class SearchCriteriaByIds
        {
            public List<int> propertyIds { get; set; }
            public List<Occupancy> occupancies { get; set; }
            public FilterBy filterBy { get; set; }

        }
        public class FilterBy
        {
            public string includeNREF { get; set; }
            public string rateType { get; set; }
        }
        public class Root
        {
            public SearchCriteriaByIds SearchCriteriaByIds { get; set; }
        }

    }

    public class HotelTraderGraphqlClient
    {
        public class GraphQLQuery
        {
            public string query { get; set; }
            public object variables { get; set; }
        }

        #region Query

        #region "searchquery"
        public static string query1 = @" query getPropertiesByIds($SearchCriteriaByIds: SearchCriteriaByIdsInput) {
  getPropertiesByIds(searchCriteriaByIds: $SearchCriteriaByIds) {
  properties {
      propertyId
      propertyName
      occupancies {
        occupancyRefId
        checkInDate
        checkOutDate
        numberOfAdults
        numberOfChildren
        childrenAges
      }
      rooms {
        occupancyRefId
        htIdentifier
        roomName
        shortDescription
        longDescription
       
        rateInfo {
          bar
          binding
          commissionable
          commissionAmount
          currency
          netPrice
          tax
          grossPrice
          payAtProperty
          dailyPrice
          dailyTax
         
          taxInfo {
            payAtBooking {
              date
              name
              description
              value
            }
            payAtProperty {
              date
              name
              description
              value
            }
          }
        }
        mealplanOptions{
          breakfastIncluded
          lunchIncluded
          dinnerIncluded
          allInclusive
          mealplanDescription
        }
        rateplanTag
        refundable
        cancellationPolicies {
          startWindowTime
          endWindowTime
          cancellationCharge
          timeZone
        }
      }
			shortDescription
			longDescription
      city
      latitude
      longitude
      starRating
      hotelImageUrl
    }
  }
}";


        #endregion



        #region "Quotequery"
        public static string queryQuote = @"query quote($Quote: [QuoteRequestInput]) {
          quote(quoteRequest: $Quote) {
            aggregateGrossPrice
            aggregateNetPrice
            aggregateTax
            aggregatePayAtProperty
            rooms {
              htIdentifier
              grossPriceChanged
              message
              cancellationPolicies {
                startWindowTime
                endWindowTime
                cancellationCharge
                timeZone
              }
              rates {
                dailyPrice
                dailyTax
                grossPrice
                netPrice
                tax
                payAtProperty
              }
            }
          }
        }";
        #endregion

        #region "Bookingquery"
        public static string Bookingquery = @"mutation book($Book: BookRequestInput) {
  book(bookRequest: $Book) {
    htConfirmationCode
    clientConfirmationCode
    otaConfirmationCode
    bookingDate
    membershipId
    specialRequests
    propertyDetails {
      ...propertyDetails
    }
    aggregateGrossPrice
    aggregateNetPrice
    aggregateTax
    aggregatePayAtProperty
    rooms {
      ...roomDetails
    }
  }
}

fragment addressDetails on Address {
  address1
  address2
  cityName
  countryCode
  stateName
  zipCode
}

fragment propertyDetails on PropertyResponseEntity {
  address {
    ...addressDetails
  }
  checkInTime
  checkOutTime
  city
  hotelImageUrl
  latitude
  longitude
  propertyId
  propertyName
  starRating
  shortDescription
  longDescription
  timeZone
  checkInPolicy
}

fragment roomDetails on RoomResponse {
  cancellationDate
  cancellationFee
  cancelled
  cancellationPolicies {
    ...cancelPolicyDetails
  }
  checkInDate
  checkOutDate
  clientRoomConfirmationCode
  htRoomConfirmationCode
  crsConfirmationCode
  crsCancelConfirmationCode
  pmsConfirmationCode
  refundable
  rateplanTag
  rateplanCode
  shortDescription
  longDescription
  roomName
  mealplanOptions {
    breakfastIncluded
    lunchIncluded
    dinnerIncluded
    allInclusive
    mealplanDescription
  }
  rates {
    ...ratesDetails
  }
  occupancy {
    childrenAges
    noOfAdults
    noOfChildren
  }
  guests {
    ...guestDetails
  }
  roomSpecialRequests
}

fragment cancelPolicyDetails on HtCancellationPolicy {
  startWindowTime
  endWindowTime
  cancellationCharge
  timeZone
}

fragment ratesDetails on RoomRatesResponseEntity {
  netPrice
  tax
  grossPrice
  dailyPrice
  dailyTax
  payAtProperty
  taxInfo {
    payAtBooking {
      date
      description
      name
      value
    }
    payAtProperty {
      date
      description
      name
      value
    }
  }
}

fragment guestDetails on RoomGuestResponseEntity {
  adult
  age
  email
  firstName
  lastName
  phone
  primary
}";
        #endregion

        #region BookingCancellationQuery
        public static string queryBkgCancellaion = @"mutation cancel($Cancel: CancelRequestInput) {
          cancel(cancelRequest: $Cancel) {
            htConfirmationCode
            clientConfirmationCode
            allRoomsCancelled
            rooms {
              htRoomConfirmationCode
              clientRoomConfirmationCode
              cancelled
              cancellationDate
            }
          }
        }";

        #endregion
        #endregion
    }

    public class HotelTraderResponse
    {
        #region HotelTrader Response Class
        public class Occupancy
        {
            public int occupancyRefId { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public int numberOfAdults { get; set; }
            public int numberOfChildren { get; set; }
            public string childrenAges { get; set; }
        }

        public class MealplanOptions
        {
            public bool breakfastIncluded { get; set; }
            public object lunchIncluded { get; set; }
            public object dinnerIncluded { get; set; }
            public object allInclusive { get; set; }
            public string mealplanDescription { get; set; }
        }

        public class PayAtBooking
        {
            public string date { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public double value { get; set; }
        }

        public class PayAtProperty
        {
            public string date { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public double value { get; set; }
        }

        public class TaxInfo
        {
            public List<PayAtBooking> payAtBooking { get; set; }
            public List<PayAtProperty> payAtProperty { get; set; }
        }

        public class RateInfo
        {
            public double? bar { get; set; }
            public bool? binding { get; set; }
            public bool? commissionable { get; set; }
            public double? commissionAmount { get; set; }
            public string currency { get; set; }
            public double netPrice { get; set; }
            public double tax { get; set; }
            public double grossPrice { get; set; }
            public double payAtProperty { get; set; }
            public List<double> dailyPrice { get; set; }
            public List<double> dailyTax { get; set; }
            public TaxInfo taxInfo { get; set; }
        }

        public class CancellationPolicy
        {
            public string startWindowTime { get; set; }
            public string endWindowTime { get; set; }
            public double cancellationCharge { get; set; }
            public string timeZone { get; set; }
        }

        public class Room
        {
            public int occupancyRefId { get; set; }
            public string htIdentifier { get; set; }
            public string roomName { get; set; }
            public MealplanOptions mealplanOptions { get; set; }
            public RateInfo rateInfo { get; set; }
            public bool refundable { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
        }

        public class Property
        {
            public int propertyId { get; set; }
            public string propertyName { get; set; }
            public List<Occupancy> occupancies { get; set; }
            public List<Room> rooms { get; set; }
            public string city { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
            public double starRating { get; set; }
            public string hotelImageUrl { get; set; }
        }

        public class GetPropertiesByIds
        {
            public List<Property> properties { get; set; }
        }

        public class Data
        {
            public GetPropertiesByIds getPropertiesByIds { get; set; }
        }

        public class Root
        {
            public Data data { get; set; }
        }


        #endregion
    }

    public class HotelTraderRequest_Quote
    {
        public class Rates
        {
            public List<double> dailyPrice { get; set; }
            public List<double> dailyTax { get; set; }
            public double grossPrice { get; set; }
            public double netPrice { get; set; }
            public double tax { get; set; }
            public double payAtProperty { get; set; }
        }

        public class Occupancy
        {
            public int numberOfAdults { get; set; }
            public int numberOfChildren { get; set; }
            public string childrenAges { get; set; }
        }

        public class Quote
        {
            public string htIdentifier { get; set; }
            public Rates rates { get; set; }
            public Occupancy occupancy { get; set; }
        }

        public class Root
        {
            public List<Quote> Quote { get; set; }
        }
    }

    public class HotelTraderQuoteResponse
    {
        #region QuoteResponse
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class CancellationPolicy
        {
            public string startWindowTime { get; set; }
            public string endWindowTime { get; set; }
            public double cancellationCharge { get; set; }
            public string timeZone { get; set; }
        }

        public class Rates
        {
            public List<double> dailyPrice { get; set; }
            public List<double> dailyTax { get; set; }
            public double grossPrice { get; set; }
            public double netPrice { get; set; }
            public double tax { get; set; }
            public double payAtProperty { get; set; }
        }

        public class Room1
        {
            public string htIdentifier { get; set; }
            public bool grossPriceChanged { get; set; }
            public string message { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
            public Rates rates { get; set; }
        }

        public class Quote
        {
            public double aggregateGrossPrice { get; set; }
            public double aggregateNetPrice { get; set; }
            public double aggregateTax { get; set; }
            public double aggregatePayAtProperty { get; set; }
            public List<Room1> rooms { get; set; }
        }

        public class Data1
        {
            public Quote quote { get; set; }
        }

        public class Root
        {
            public Data1 data { get; set; }
        }

        #endregion
    }

    public class HotelTraderCancelRequest
    {
        #region Booking Cancellation Request
        public class BookingCancellation_Request
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
            public class Cancel
            {
                public string htConfirmationCode { get; set; }
            }

            public class Root
            {
                public Cancel Cancel { get; set; }
            }


        }
        #endregion
    }

    public class HotelTraderBookingRequest
    {
        #region Booking Request
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class HotelTraderHTBookingReq
        {
            public class PaymentInformation
            {
                public string address { get; set; }
                public string address2 { get; set; }
                public string cardNumber { get; set; }
                public string cardType { get; set; }
                public string city { get; set; }
                public string countryCode { get; set; }
                public string email { get; set; }
                public string expiryMonth { get; set; }
                public string expiryYear { get; set; }
                public string firstName { get; set; }
                public string lastName { get; set; }
                public string currencyCode { get; set; }
                public int paymentAmount { get; set; }
                public int securityCode { get; set; }
                public string state { get; set; }
                public int zip { get; set; }
            }

            public class Rates
            {
                public List<double> dailyPrice { get; set; }
                public List<double> dailyTax { get; set; }
                public double netPrice { get; set; }
                public double tax { get; set; }
                public double payAtProperty { get; set; }
            }

            public class Occupancy
            {
                public int numberOfAdults { get; set; }
                public int numberOfChildren { get; set; }
                public object childrenAges { get; set; }
            }

            public class Guest
            {
                public string firstName { get; set; }
                public string lastName { get; set; }
                public string email { get; set; }
                public bool adult { get; set; }
                public int age { get; set; }
                public string phone { get; set; }
                public bool primary { get; set; }
            }

            public class Room
            {
                public string htIdentifier { get; set; }
                public string clientRoomConfirmationCode { get; set; }
                public List<string> roomSpecialRequests { get; set; }
                public Rates rates { get; set; }
                public Occupancy occupancy { get; set; }
                public List<Guest> guests { get; set; }
            }

            public class Book
            {
                public string clientConfirmationCode { get; set; }
                public string otaConfirmationCode { get; set; }
                public string otaClientName { get; set; }
                public object membershipId { get; set; }
                public List<string> specialRequests { get; set; }
                public PaymentInformation paymentInformation { get; set; }
                public List<Room> rooms { get; set; }
            }

            public class Root
            {
                public Book Book { get; set; }
            }
        }

        #endregion
    }

    public class HotelTraderBookingResponse
    {

        //#region Booking Response

        //public class HotelTraderBookingRes
        //{
        //    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        //    public class Address
        //    {
        //        public string address1 { get; set; }
        //        public object address2 { get; set; }
        //        public string cityName { get; set; }
        //        public string countryCode { get; set; }
        //        public string stateName { get; set; }
        //        public string zipCode { get; set; }
        //    }

        //    public class PropertyDetails
        //    {
        //        public Address address { get; set; }
        //        public string checkInTime { get; set; }
        //        public string checkOutTime { get; set; }
        //        public string city { get; set; }
        //        public string hotelImageUrl { get; set; }
        //        public string latitude { get; set; }
        //        public string longitude { get; set; }
        //        public int propertyId { get; set; }
        //        public string propertyName { get; set; }
        //        public double starRating { get; set; }
        //    }

        //    public class MealPlanOption
        //    {
        //        public bool breakfastIncluded { get; set; }
        //        public bool lunchIncluded { get; set; }
        //        public bool dinnerIncluded { get; set; }
        //        public bool allInclusive { get; set; }
        //        public string mealplanDescription { get; set; }
        //    }

        //    public class PayAtBooking
        //    {
        //        public string date { get; set; }
        //        public string description { get; set; }
        //        public string name { get; set; }
        //        public double value { get; set; }
        //    }

        //    public class PayAtProperty
        //    {
        //        public string date { get; set; }
        //        public string description { get; set; }
        //        public string name { get; set; }
        //        public double value { get; set; }
        //    }

        //    public class TaxInfo
        //    {
        //        public List<PayAtBooking> payAtBooking { get; set; }
        //        public List<PayAtProperty> payAtProperty { get; set; }
        //    }

        //    public class Rates
        //    {
        //        public double netPrice { get; set; }
        //        public double tax { get; set; }
        //        public double grossPrice { get; set; }
        //        public List<double> dailyPrice { get; set; }
        //        public List<double> dailyTax { get; set; }
        //        public double payAtProperty { get; set; }
        //        public TaxInfo taxInfo { get; set; }
        //    }

        //    public class Occupancy
        //    {
        //        public object childrenAges { get; set; }
        //        public int noOfAdults { get; set; }
        //        public int noOfChildren { get; set; }
        //    }

        //    public class Guest
        //    {
        //        public bool adult { get; set; }
        //        public object age { get; set; }
        //        public string email { get; set; }
        //        public string firstName { get; set; }
        //        public string lastName { get; set; }
        //        public string phone { get; set; }
        //        public bool primary { get; set; }
        //    }

        //    public class Room
        //    {
        //        public object cancellationDate { get; set; }
        //        public object cancellationFee { get; set; }
        //        public bool cancelled { get; set; }
        //        public object cancellationPolicies { get; set; }
        //        public string checkInDate { get; set; }
        //        public string checkOutDate { get; set; }
        //        public string clientRoomConfirmationCode { get; set; }
        //        public string htRoomConfirmationCode { get; set; }
        //        public object crsConfirmationCode { get; set; }
        //        public object crsCancelConfirmationCode { get; set; }
        //        public object pmsConfirmationCode { get; set; }
        //        public bool refundable { get; set; }
        //        public string roomName { get; set; }
        //        public List<MealPlanOption> mealPlanOptions { get; set; }
        //        public Rates rates { get; set; }
        //        public Occupancy occupancy { get; set; }
        //        public List<Guest> guests { get; set; }
        //        public object roomSpecialRequests { get; set; }
        //    }

        //    public class Book
        //    {
        //        public string htConfirmationCode { get; set; }
        //        public string clientConfirmationCode { get; set; }
        //        public string otaConfirmationCode { get; set; }
        //        public string bookingDate { get; set; }
        //        public object membershipId { get; set; }
        //        public object specialRequests { get; set; }
        //        public PropertyDetails propertyDetails { get; set; }
        //        public double aggregateGrossPrice { get; set; }
        //        public double aggregateNetPrice { get; set; }
        //        public double aggregateTax { get; set; }
        //        public double aggregatePayAtProperty { get; set; }
        //        public List<Room> rooms { get; set; }
        //    }

        //    public class Data
        //    {
        //        public Book book { get; set; }
        //    }

        //    public class Root
        //    {
        //        public Data data { get; set; }
        //    }

        //}
        //#endregion

        #region Booking Response
        public class HotelTraderBookingRes
        {
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Address
            {
                public string address1 { get; set; }
                public object address2 { get; set; }
                public string cityName { get; set; }
                public string countryCode { get; set; }
                public string stateName { get; set; }
                public string zipCode { get; set; }
            }

            public class PropertyDetails
            {
                public Address address { get; set; }
                public string checkInTime { get; set; }
                public string checkOutTime { get; set; }
                public string city { get; set; }
                public string hotelImageUrl { get; set; }
                public string latitude { get; set; }
                public string longitude { get; set; }
                public int propertyId { get; set; }
                public string propertyName { get; set; }
                public double starRating { get; set; }
                public string shortDescription { get; set; }
                public string longDescription { get; set; }
                public string timeZone { get; set; }
                public string checkInPolicy { get; set; }
            }

            public class CancellationPolicy
            {
                public string startWindowTime { get; set; }
                public string endWindowTime { get; set; }
                public double cancellationCharge { get; set; }
                public string timeZone { get; set; }
            }

            public class MealplanOptions
            {
                public bool breakfastIncluded { get; set; }
                public bool lunchIncluded { get; set; }
                public bool dinnerIncluded { get; set; }
                public bool allInclusive { get; set; }
                public object mealplanDescription { get; set; }
            }

            public class PayAtBooking
            {
                public string date { get; set; }
                public string description { get; set; }
                public string name { get; set; }
                public double value { get; set; }
            }

            public class PayAtProperty
            {
                public string date { get; set; }
                public string description { get; set; }
                public string name { get; set; }
                public double value { get; set; }
            }

            public class TaxInfo
            {
                public List<PayAtBooking> payAtBooking { get; set; }
                public List<PayAtProperty> payAtProperty { get; set; }
            }

            public class Rates
            {
                public double netPrice { get; set; }
                public double tax { get; set; }
                public double grossPrice { get; set; }
                public List<double> dailyPrice { get; set; }
                public List<double> dailyTax { get; set; }
                public double payAtProperty { get; set; }
                public TaxInfo taxInfo { get; set; }
            }

            public class Occupancy
            {
                public object childrenAges { get; set; }
                public int noOfAdults { get; set; }
                public int noOfChildren { get; set; }
            }

            public class Guest
            {
                public bool adult { get; set; }
                public object age { get; set; }
                public string email { get; set; }
                public string firstName { get; set; }
                public string lastName { get; set; }
                public string phone { get; set; }
                public bool primary { get; set; }
            }

            public class Room
            {
                public object cancellationDate { get; set; }
                public object cancellationFee { get; set; }
                public bool cancelled { get; set; }
                public List<CancellationPolicy> cancellationPolicies { get; set; }
                public string checkInDate { get; set; }
                public string checkOutDate { get; set; }
                public string clientRoomConfirmationCode { get; set; }
                public string htRoomConfirmationCode { get; set; }
                public object crsConfirmationCode { get; set; }
                public object crsCancelConfirmationCode { get; set; }
                public object pmsConfirmationCode { get; set; }
                public bool refundable { get; set; }
                public object rateplanTag { get; set; }
                public string rateplanCode { get; set; }
                public string shortDescription { get; set; }
                public string longDescription { get; set; }
                public string roomName { get; set; }
                public MealplanOptions mealplanOptions { get; set; }
                public Rates rates { get; set; }
                public Occupancy occupancy { get; set; }
                public List<Guest> guests { get; set; }
                public object roomSpecialRequests { get; set; }
            }

            public class Book
            {
                public string htConfirmationCode { get; set; }
                public string clientConfirmationCode { get; set; }
                public string otaConfirmationCode { get; set; }
                public DateTime bookingDate { get; set; }
                public object membershipId { get; set; }
                public object specialRequests { get; set; }
                public PropertyDetails propertyDetails { get; set; }
                public double aggregateGrossPrice { get; set; }
                public double aggregateNetPrice { get; set; }
                public double aggregateTax { get; set; }
                public double aggregatePayAtProperty { get; set; }
                public List<Room> rooms { get; set; }
            }

            public class Data
            {
                public Book book { get; set; }
            }

            public class Root
            {
                public Data data { get; set; }
            }
        }

        #endregion
    }


    public class HotelTraderCancelResponse
    {
        #region Cancallation Response 
        public class BookingCancellation_Response
        {

            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
            public class Room
            {
                public string htRoomConfirmationCode { get; set; }
                public string clientRoomConfirmationCode { get; set; }
                public bool cancelled { get; set; }
                public string cancellationDate { get; set; }
            }

            public class Cancel
            {
                public string htConfirmationCode { get; set; }
                public string clientConfirmationCode { get; set; }
                public bool allRoomsCancelled { get; set; }
                public List<Room> rooms { get; set; }
            }

            public class Data
            {
                public Cancel cancel { get; set; }
            }

            public class Root
            {
                public Data data { get; set; }
            }


        }
        #endregion
    }


    #endregion
}
