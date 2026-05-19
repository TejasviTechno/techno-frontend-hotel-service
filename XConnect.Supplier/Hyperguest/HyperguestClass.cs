using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static XConnect.Supplier.Agoda.Agoda_newBookingRQ;
using static XConnect.Supplier.GRNC.GRNCsearch;

namespace XConnect.Supplier.Hyperguest
{
    public class HyperguestClass
    {
        #region hyperguest class
        public class hyperguest
        {
            // public static string hyperguest_Currency = "USD";

            #region roomlist
            public class RoomList
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                public class HotelInfo
                {
                    public string name { get; set; }
                    public object starRating { get; set; }
                    public string cityName { get; set; }
                    public int cityId { get; set; }
                    public double longitude { get; set; }
                    public double latitude { get; set; }
                    public int propertyType { get; set; }
                }

                public class Pax
                {
                    public int adults { get; set; }
                    public List<object> children { get; set; }
                }

                public class BeddingConfiguration
                {
                    public string type { get; set; }
                    public string size { get; set; }
                    public int quantity { get; set; }
                }

                public class InclusivePrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class ExclusivePrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class CommissionValue
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class CancellationPolicy
                {
                    public double daysBefore { get; set; }
                    public string penaltyType { get; set; }
                    public double amount { get; set; }
                }

                public class PropertyFee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class MandatoryTax
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class MandatoryFee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class Fees
                {
                    public List<MandatoryTax> mandatoryTaxes { get; set; }
                    public List<MandatoryFee> mandatoryFees { get; set; }
                }

                public class NightlyBreakdown
                {
                    public string date { get; set; }
                    public InclusivePrice inclusivePrice { get; set; }
                    public ExclusivePrice exclusivePrice { get; set; }
                    public List<PropertyFee> propertyFees { get; set; }
                    public Fees fees { get; set; }
                    public double commissionValue { get; set; }
                    public string commissionChargeType { get; set; }
                    public string commissionCalculation { get; set; }
                }

                public class BarPrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class RatePlan
                {
                    public string ratePlanCode { get; set; }
                    public object ratePlanId { get; set; }
                    public string board { get; set; }
                    public string payment { get; set; }
                    public string priceType { get; set; }
                    public string charge { get; set; }
                    public InclusivePrice inclusivePrice { get; set; }
                    public ExclusivePrice exclusivePrice { get; set; }
                    public CommissionValue commissionValue { get; set; }
                    public List<object> remarks { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public List<NightlyBreakdown> nightlyBreakdown { get; set; }
                    public List<PropertyFee> propertyFees { get; set; }
                    public bool isImmediate { get; set; }
                    public Fees fees { get; set; }
                    public BarPrice barPrice { get; set; }
                }

                public class Room
                {
                    public Pax pax { get; set; }
                    public string roomTypeCode { get; set; }
                    public int numberOfAvailableRooms { get; set; }
                    public string roomName { get; set; }
                    public int roomId { get; set; }
                    public List<BeddingConfiguration> beddingConfigurations { get; set; }
                    public List<RatePlan> ratePlans { get; set; }
                }

                public class Result
                {
                    public string hotelId { get; set; }
                    public HotelInfo hotelInfo { get; set; }
                    public List<object> remarks { get; set; }
                    public List<Room> rooms { get; set; }
                }

                public class Root
                {
                    public List<Result> results { get; set; }
                    public DateTime timeStamp { get; set; }
                }
            }
            #endregion

            #region hotellist
            public class HotelList
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                public class HotelInfo
                {
                    public string name { get; set; }
                    public object starRating { get; set; }
                    public string cityName { get; set; }
                    public int cityId { get; set; }
                    public double longitude { get; set; }
                    public double latitude { get; set; }
                    public int propertyType { get; set; }
                }

                public class Pax
                {
                    public int adults { get; set; }
                    public List<object> children { get; set; }
                }

                public class BeddingConfiguration
                {
                    public string type { get; set; }
                    public string size { get; set; }
                    public int quantity { get; set; }
                }

                public class InclusivePrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class ExclusivePrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class CommissionValue
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class CancellationPolicy
                {
                    public double daysBefore { get; set; }
                    public string penaltyType { get; set; }
                    public double amount { get; set; }
                }

                public class PropertyFee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class MandatoryTax
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class MandatoryFee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class Fees
                {
                    public List<MandatoryTax> mandatoryTaxes { get; set; }
                    public List<MandatoryFee> mandatoryFees { get; set; }
                }

                public class NightlyBreakdown
                {
                    public string date { get; set; }
                    public InclusivePrice inclusivePrice { get; set; }
                    public ExclusivePrice exclusivePrice { get; set; }
                    public List<PropertyFee> propertyFees { get; set; }
                    public Fees fees { get; set; }
                    public double commissionValue { get; set; }
                    public string commissionChargeType { get; set; }
                    public string commissionCalculation { get; set; }
                }

                public class BarPrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class RatePlan
                {
                    public string ratePlanCode { get; set; }
                    public object ratePlanId { get; set; }
                    public string board { get; set; }
                    public string payment { get; set; }
                    public string priceType { get; set; }
                    public string charge { get; set; }
                    public InclusivePrice inclusivePrice { get; set; }
                    public ExclusivePrice exclusivePrice { get; set; }
                    public CommissionValue commissionValue { get; set; }
                    public List<object> remarks { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public List<NightlyBreakdown> nightlyBreakdown { get; set; }
                    public List<PropertyFee> propertyFees { get; set; }
                    public bool isImmediate { get; set; }
                    public Fees fees { get; set; }
                    public BarPrice barPrice { get; set; }
                }

                public class Room
                {
                    public Pax pax { get; set; }
                    public string roomTypeCode { get; set; }
                    public int numberOfAvailableRooms { get; set; }
                    public string roomName { get; set; }
                    public int roomId { get; set; }
                    public List<BeddingConfiguration> beddingConfigurations { get; set; }
                    public List<RatePlan> ratePlans { get; set; }
                }

                public class Result
                {
                    public string hotelId { get; set; }
                    public HotelInfo hotelInfo { get; set; }
                    public List<object> remarks { get; set; }
                    public List<Room> rooms { get; set; }
                }

                public class Root
                {
                    public List<Result> results { get; set; }
                    public DateTime timeStamp { get; set; }
                }
            }
            #endregion

            #region "class change"
            public class HotelList1
            {
                public class Bar
                {
                    public double price { get; set; }
                    public double searchCurrency { get; set; }
                    public string currency { get; set; }
                }

                public class BeddingConfiguration
                {
                    public string type { get; set; }
                    public String size { get; set; }
                    public int quantity { get; set; }
                }

                public class Billing
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Calculation
                {
                    public string type { get; set; }
                    public double value { get; set; }
                    public double? searchCurrency { get; set; }
                }

                public class CancellationPolicy
                {
                    public int daysBefore { get; set; }
                    public string penaltyType { get; set; }
                    public double amount { get; set; }
                    public TimeSetting timeSetting { get; set; }
                    public string cancellationDeadlineHour { get; set; }
                }

                public class ChargeAmount
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                    public double searchCurrency { get; set; }
                    public Billing billing { get; set; }
                    public Origin origin { get; set; }
                    public Search search { get; set; }
                }

                public class Commission
                {
                    public double price { get; set; }
                    public double searchCurrency { get; set; }
                    public string currency { get; set; }
                }

                public class Contract
                {
                    public int contractId { get; set; }
                    public string type { get; set; }
                    public List<Term> terms { get; set; }
                }

                public class Fee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public double searchCurrency { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                    public Calculation calculation { get; set; }
                }

                public class Net
                {
                    public double price { get; set; }
                    public double searchCurrency { get; set; }
                    public string currency { get; set; }
                    public List<Taxis> taxes { get; set; }
                }

                public class NightlyBreakdown
                {
                    public string date { get; set; }
                    public Prices prices { get; set; }
                }

                public class Origin
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Payment
                {
                    public string charge { get; set; }
                    public string chargeType { get; set; }
                    public ChargeAmount chargeAmount { get; set; }
                }

                public class Prices
                {
                    public Net net { get; set; }
                    public Sell sell { get; set; }
                    public Commission commission { get; set; }
                    public Bar bar { get; set; }
                    public List<Fee> fees { get; set; }
                }

                public class PropertyInfo
                {
                    public string name { get; set; }
                    public int starRating { get; set; }
                    public string cityName { get; set; }
                    public int cityId { get; set; }
                    public string countryCode { get; set; }
                    public string regionName { get; set; }
                    public double longitude { get; set; }
                    public double latitude { get; set; }
                    public int propertyType { get; set; }
                    public string propertyTypeName { get; set; }
                }

                public class RatePlan
                {
                    public string ratePlanCode { get; set; }
                    public string ratePlanName { get; set; }
                    public double ratePlanId { get; set; }
                    public RatePlanInfo ratePlanInfo { get; set; }
                    public string board { get; set; }
                    public Payment payment { get; set; }
                    public Prices prices { get; set; }
                    public List<string> remarks { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public List<NightlyBreakdown> nightlyBreakdown { get; set; }
                    public bool isImmediate { get; set; }
                }

                public class RatePlanInfo
                {
                    public bool @virtual { get; set; }
                    public List<Contract> contracts { get; set; }
                    public List<Term> terms { get; set; }
                    public string originalRatePlanCode { get; set; }
                    public bool isPromotion { get; set; }
                    public bool isPackageRate { get; set; }
                    public bool isPrivate { get; set; }
                    public string contractId { get; set; }
                }

                public class Result
                {
                    public int propertyId { get; set; }
                    public PropertyInfo propertyInfo { get; set; }
                    public List<string> remarks { get; set; }
                    public List<Room> rooms { get; set; }
                }

                public class Room
                {
                    public SearchedPax searchedPax { get; set; }
                    public string roomTypeCode { get; set; }
                    public int numberOfAvailableRooms { get; set; }
                    public string roomName { get; set; }
                    public int roomId { get; set; }
                    public Settings settings { get; set; }
                    public List<RatePlan> ratePlans { get; set; }
                }

                public class Root
                {
                    public List<Result> results { get; set; }
                    public DateTime timeStamp { get; set; }
                }

                public class Search
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class SearchedPax
                {
                    public int adults { get; set; }
                    public List<object> children { get; set; }
                }

                public class Sell
                {
                    public double price { get; set; }
                    public double searchCurrency { get; set; }
                    public string currency { get; set; }
                    public List<Taxis> taxes { get; set; }
                }

                public class Settings
                {
                    public int numberOfBedrooms { get; set; }
                    public int? roomSize { get; set; }
                    public int maxAdultsNumber { get; set; }
                    public int maxChildrenNumber { get; set; }
                    public int maxInfantsNumber { get; set; }
                    public int maxOccupancy { get; set; }
                    public int numberOfBeds { get; set; }
                    public List<BeddingConfiguration> beddingConfigurations { get; set; }
                }

                public class Taxis
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public double searchCurrency { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                    public Calculation calculation { get; set; }
                }

                public class Term
                {
                    public int id { get; set; }
                    public string name { get; set; }
                    public bool isPromotion { get; set; }
                    public int contractId { get; set; }
                    public string type { get; set; }
                    public List<int> termIds { get; set; }
                }

                public class TimeSetting
                {
                    public int timeFromCheckIn { get; set; }
                    public string timeFromCheckInType { get; set; }
                }
            }

            #endregion
            #region pricecheck resp_newversion2
            public class PriceCheckRespNew
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                public class Bar
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                    public List<Taxis> taxes { get; set; }
                }

                public class CancellationPolicy
                {
                    public int daysBefore { get; set; }
                    public string penaltyType { get; set; }
                    public double amount { get; set; }
                    public TimeSetting timeSetting { get; set; }
                    public string cancellationDeadlineHour { get; set; }
                }

                public class ChargeAmount
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Commission
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Content
                {
                    public List<PaymentOption> paymentOptions { get; set; }
                    public List<Room> rooms { get; set; }
                }

                public class FromAmount
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class Net
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                    public List<Taxis> taxes { get; set; }
                }

                public class NightlyRate
                {
                    public string date { get; set; }
                    public Prices prices { get; set; }
                }

                public class Payment
                {
                    public string charge { get; set; }
                    public string chargeType { get; set; }
                    public ChargeAmount chargeAmount { get; set; }
                }

                public class PaymentAmount
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class PaymentOption
                {
                    public string type { get; set; }
                    public PaymentAmount paymentAmount { get; set; }
                    public PriceChange priceChange { get; set; }
                }


                public class PriceChange
                {
                    public FromAmount fromAmount { get; set; }
                    public ToAmount toAmount { get; set; }
                }

                public class Prices
                {
                    public Net net { get; set; }
                    public Sell sell { get; set; }
                    public Commission commission { get; set; }
                    public Bar bar { get; set; }
                    public List<object> fees { get; set; }
                }

                public class Room
                {
                    public Payment payment { get; set; }
                    public Prices prices { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public List<object> remarks { get; set; }
                    public string roomTypeCode { get; set; }
                    public int roomId { get; set; }
                    public string ratePlanCode { get; set; }
                    public string ratePlanId { get; set; }
                    public List<NightlyRate> nightlyRates { get; set; }
                }

                public class Root
                {
                    public Content content { get; set; }
                    public DateTime timestamp { get; set; }
                }

                public class Sell
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                    public List<Taxis> taxes { get; set; }
                }

                public class Taxis
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class TimeSetting
                {
                    public int timeFromCheckIn { get; set; }
                    public string timeFromCheckInType { get; set; }
                }

                public class ToAmount
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

            }
            #endregion
            #region pricechek req_new
            public class Pricecheck
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                public class Dates
                {
                    public string from { get; set; }
                    public string to { get; set; }
                }

                public class ExpectedPrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class Pax
                {
                    public int adults { get; set; }
                    public List<int> children { get; set; }
                }

                public class Room
                {
                    public string roomCode { get; set; }
                    public string rateCode { get; set; }
                    public ExpectedPrice expectedPrice { get; set; }
                }
                public class Search
                {
                    public Dates dates { get; set; }
                    public int hotelId { get; set; }
                    public List<Pax> pax { get; set; }
                }
                public class Root
                {
                    public Search search { get; set; }
                    public List<Room> rooms { get; set; }
                }


            }
            #endregion

            #region pricecheck resp_new
            public class PriceCheckResp
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                public class CancellationPolicy
                {
                    public double daysBefore { get; set; }
                    public string penaltyType { get; set; }
                    public double amount { get; set; }
                }

                public class CommissionValue
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class Content
                {
                    public List<PaymentOption> paymentOptions { get; set; }
                    public List<Room> rooms { get; set; }
                }

                public class ExclusivePrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class Fees
                {
                    public List<MandatoryTaxis> mandatoryTaxes { get; set; }
                    public List<MandatoryFee> mandatoryFees { get; set; }
                }

                public class FromAmount
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class InclusivePrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class MandatoryFee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class MandatoryTaxis
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class NightlyRate
                {
                    public string date { get; set; }
                    public InclusivePrice inclusivePrice { get; set; }
                    public ExclusivePrice exclusivePrice { get; set; }
                    public double commissionValue { get; set; }
                    public string commissionChargeType { get; set; }
                    public string commissionCalculation { get; set; }
                    public List<PropertyFee> propertyFees { get; set; }
                    public Fees fees { get; set; }
                }

                public class PaymentAmount
                {
                    public Price price { get; set; }
                    public CommissionValue commissionValue { get; set; }
                }

                public class PaymentOption
                {
                    public string type { get; set; }
                    public PaymentAmount paymentAmount { get; set; }
                }

                public class Price
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class PriceChange
                {
                    public FromAmount fromAmount { get; set; }
                    public ToAmount toAmount { get; set; }
                }

                public class PropertyFee
                {
                    public string description { get; set; }
                    public double amount { get; set; }
                    public string currency { get; set; }
                    public string relation { get; set; }
                    public string scope { get; set; }
                    public string frequency { get; set; }
                }

                public class Room
                {
                    public PriceChange priceChange { get; set; }
                    public InclusivePrice inclusivePrice { get; set; }
                    public ExclusivePrice exclusivePrice { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public List<string> searchRemarks { get; set; }
                    public string roomCode { get; set; }
                    public int roomId { get; set; }
                    public string rateCode { get; set; }
                    public double ratePlanId { get; set; } //change int to double
                    public List<NightlyRate> nightlyRates { get; set; }
                }

                public class Root
                {
                    public Content content { get; set; }
                    public DateTime timestamp { get; set; }
                }

                public class ToAmount
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

            }
            #endregion

            #region BookigCancelReq

            public class BookingCancelationReq
            {
                public class Root
                {
                    public int bookingId { get; set; }
                    public string reason { get; set; }
                    public bool simulation { get; set; }
                }

            }
            #endregion

            #region BookingResp

            public class HyperguestBookingResp
            {
                public class Dates
                {
                    public string from { get; set; }
                    public string to { get; set; }
                }

                public class Reference
                {
                    public string hotel { get; set; }
                    public string agency { get; set; }
                }

                public class Price
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class CancellationPolicy
                {
                    public int policyId { get; set; }
                    public string startDate { get; set; }
                    public object endDate { get; set; }
                    public Price price { get; set; }
                }

                public class Name
                {
                    public string first { get; set; }
                    public string last { get; set; }
                }

                public class Contact
                {
                    public string address { get; set; }
                    public string city { get; set; }
                    public string country { get; set; }
                    public string nationality { get; set; }
                    public string email { get; set; }
                    public string phone { get; set; }
                    public string state { get; set; }
                    public string zip { get; set; }
                }

                public class Guest
                {
                    public int guestId { get; set; }
                    public DateTime birthDate { get; set; }
                    public Name name { get; set; }
                    public Contact contact { get; set; }
                    public string title { get; set; }
                    public int age { get; set; }
                }

                public class NetPrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }

                public class Room
                {
                    public Reference reference { get; set; }
                    public List<CancellationPolicy> cancellationPolicy { get; set; }
                    public List<Guest> guests { get; set; }
                    public List<object> specialRequests { get; set; }
                    public List<string> remarks { get; set; }
                    public int itemId { get; set; }
                    public int roomId { get; set; }
                    public int hotelId { get; set; }
                    public int ratePlanId { get; set; }
                    public string status { get; set; }
                    public string board { get; set; }
                    public Price price { get; set; }
                    public NetPrice netPrice { get; set; }
                    public List<List<object>> nightly { get; set; }
                }

                public class LeadGuest
                {
                    public int guestId { get; set; }
                    public DateTime birthDate { get; set; }
                    public Name name { get; set; }
                    public Contact contact { get; set; }
                    public string title { get; set; }
                    public int age { get; set; }
                }

                public class Content
                {
                    public string status { get; set; }
                    public Dates dates { get; set; }
                    public List<Room> rooms { get; set; }
                    public int bookingId { get; set; }
                    public int hotelId { get; set; }
                    public Reference reference { get; set; }
                    public LeadGuest leadGuest { get; set; }
                    public List<object> transactions { get; set; }
                }

                public class Root
                {
                    public Content content { get; set; }
                    public DateTime timestamp { get; set; }
                }

            }
            #endregion

            #region BookingReq

            public class hyperguestBookingReq
            {
                public class Dates
                {
                    public string from { get; set; }
                    public string to { get; set; }
                }
                public class Contact
                {
                    public string address { get; set; }
                    public string city { get; set; }
                    public string email { get; set; }
                    public string phone { get; set; }
                    public string state { get; set; }
                    public string zip { get; set; }
                    public string country { get; set; }
                }
                public class Name
                {
                    public string first { get; set; }
                    public string last { get; set; }
                }
                public class LeadGuest
                {
                    public string birthDate { get; set; }
                    public Contact contact { get; set; }
                    public Name name { get; set; }
                    public string title { get; set; }
                }
                public class Reference
                {
                    public string agency { get; set; }
                }
                public class Expiry
                {
                    public string month { get; set; }
                    public string year { get; set; }
                }
                public class CreditCard
                {
                    public string number { get; set; }
                    public string cvv { get; set; }
                    public Expiry expiry { get; set; }
                    public Name name { get; set; }
                }
                public class ExpectedPrice
                {
                    public double amount { get; set; }
                    public string currency { get; set; }
                }
                public class Guest
                {
                    public string birthDate { get; set; }
                    public Contact contact { get; set; }
                    public Name name { get; set; }
                    public string title { get; set; }
                }
                public class Room
                {
                    public string roomCode { get; set; }
                    public string rateCode { get; set; }
                    public ExpectedPrice expectedPrice { get; set; }
                    public List<string> specialRequests { get; set; }
                    public List<Guest> guests { get; set; }
                }

                public class PaymentDetails
                {
                    public string type { get; set; }
                    public Details details { get; set; }
                }

                public class Details
                {
                    public string number { get; set; }
                    public string cvv { get; set; }
                    public Expiry expiry { get; set; }
                    public Name name { get; set; }
                }
                public class Root
                {
                    public int propertyId { get; set; }
                   
                    public Dates dates { get; set; }
                    public int hotelId { get; set; }
                    public LeadGuest leadGuest { get; set; }
                    public Reference reference { get; set; }
                    public CreditCard creditCard { get; set; }
                    public List<Room> rooms { get; set; }
                    public PaymentDetails paymentDetails { get; set; }
                    
                }
            }
            #endregion


        }
        #endregion
    }
}
