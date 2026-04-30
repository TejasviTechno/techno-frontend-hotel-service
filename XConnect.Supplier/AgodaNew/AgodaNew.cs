using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using XConnect.Expedia;
using XConnect.Models;

namespace XConnect.Supplier.AgodaNew
{
    //public class AgodaNew
    //{

    //    public class AgodaBookingIDsRQ
    //    {
    //        public DateTime? from { get; set; }
    //        public DateTime? to { get; set; }
    //    }

    //    public class AgodaSearchError
    //    {
    //        public string responseStatus { get; set; }
    //        public string version { get; set; }
    //        public string remark { get; set; }

    //    }

    //    public class AgodaAuthResult
    //    {
    //        public bool IsUnauthorized { get; set; }
    //        public TokenData TokenData { get; set; }
    //        public CommonRequest CommonRequest { get; set; }
    //    }

    //    public class AgodaBookingIDsRS
    //    {
    //        public string responseStatus { get; set; }
    //        public string version { get; set; }
    //        public List<BookingId> bookingIds { get; set; }
    //    }

    //    public class BookingId
    //    {
    //        public string supplierBookingId { get; set; }
    //        public string bookingReferenceId { get; set; }
    //        public string bookingStatus { get; set; }
    //    }

    //    public class AgodaVersion
    //    {
    //        public const string Version = "v1.9";
    //    }

    //    public class AgodaBookingDetailRQ
    //    {
    //        public string bookingReferenceId { get; set; }
    //    }

    //    public class AgodaBookingDetailRS
    //    {
    //        public string responseStatus { get; set; }
    //        public string version { get; set; }
    //        public string bookingReferenceId { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string supplierBookingId { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string bookingStatus { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string hotel { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string checkIn { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string checkOut { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string roomCode { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string boardCode { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string ratePlanCode { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public int channel { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public int quantity { get; set; }
    //        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        //public string specialRequests { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public GuestCount guestCount { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public Price price { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public CancellationInfo cancellationInfo { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public List<int> benefits { get; set; }
    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string hotelConfirmationReference { get; set; }

    //        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //        public string remark { get; set; }
    //    }

    //    public class GuestCount
    //    {
    //        public int adults { get; set; }
    //        public List<int> childrenAges { get; set; }
    //    }

    //    public class Price
    //    {
    //        public double payoutPrice { get; set; }
    //        public string currency { get; set; }
    //        public double commissionPercentage { get; set; }
    //    }
    //    public class CancellationInfo
    //    {
    //        public decimal cancellationFee { get; set; }
    //        public string cancellationDate { get; set; }
    //    }

    //    public class ClientData
    //    {
    //        public DateTime? Timestamp { get; set; }
    //        public DateTime? SearchedDate { get; set; }
    //        public string AgentName { get; set; } = null;
    //        public string HotelIds { get; set; } = null;
    //        public string CityId { get; set; }
    //        public string ErrorCode { get; set; }
    //        public string CheckIn { get; set; } = null;
    //        public string CheckOut { get; set; } = null;
    //        public string Nationality { get; set; } = null;
    //        public int NoOfAdult { get; set; }
    //        public int NoOfChild { get; set; }
    //        public int NoOfNights { get; set; }
    //        public string ApiName { get; set; } = null;
    //        public string ElapsedTime { get; set; } = null;
    //        public int SpTimeout { get; set; }
    //        public double? ElapsedSeconds { get; set; }
    //        public string RequestId { get; set; } = null;
    //        public string Request { get; set; } = null;
    //        public int StatusFlag { get; set; }
    //        public string Token { get; set; } = null;
    //        public string IpAddress { get; set; } = null;
    //        public int? AgentId { get; set; }
    //        public int? IsSiteminder { get; set; }
    //        public int? IsRateGain { get; set; }

    //        public string description { get; set; } = null;
    //        //  public BigQueryInsertRow ToBigQueryInsertRow()
    //        //  {
    //        //      var row = new BigQueryInsertRow
    //        //{
    //        //    {"searchedate", SearchedDate},
    //        //    {"agentName", AgentName},
    //        //    { "hotelIDs", HotelIds},
    //        //    {"cityId", CityId},
    //        //    {"checkInDate", CheckIn},
    //        //    { "checkOutDate",CheckOut},
    //        //    { "nationality",Nationality},
    //        //    { "noOfAdult",NoOfAdult},
    //        //    { "noOfChild",NoOfChild},
    //        //    { "noOfNight",NoOfNights},
    //        //    { "apiName",ApiName},
    //        //    { "errorCode",ErrorCode},
    //        //    { "elapsedTime", ElapsedTime},
    //        //    { "sP_TimeOut", SpTimeout},
    //        //    { "requestId", RequestId},
    //        //    { "elapsed_seconds", ElapsedSeconds},
    //        //    { "request", Request},
    //        //    { "statusflag",StatusFlag},
    //        //    { "token", Token},
    //        //    { "ipAddress", IpAddress},
    //        //    { "agentId", AgentId},
    //        //    { "isSiteminder",IsSiteminder},
    //        //    { "isRateGain",IsRateGain},
    //        //    { "description",description},
    //        //};
    //        //      return row;
    //        //  }
    //    }

    //    public class AgodaBookingRQ
    //    {
    //        public string bookingReferenceId { get; set; }
    //        public string checkIn { get; set; }
    //        public string checkOut { get; set; }
    //        public string hotel { get; set; }
    //        public string roomCode { get; set; }
    //        public string boardCode { get; set; }
    //        public string ratePlanCode { get; set; }
    //        public int channel { get; set; }
    //        public string currency { get; set; }
    //        public int quantity { get; set; }
    //        public decimal totalPrice { get; set; }
    //        public Guest guest { get; set; }
    //        public List<RoomBook> rooms { get; set; }
    //        public string token { get; set; }
    //        public TokenData TokenData { get; set; }

    //        public string metadata { get; set; }
    //    }

    //    public class Guest
    //    {
    //        public string firstName { get; set; }
    //        public string lastName { get; set; }
    //    }

    //    /*public enum ErrorCode
    //    {
    //        NoError,
    //        DUPLICATION,
    //        SomeError,
    //        AuthorizationError
    //    }*/

    //    public class BookingResult
    //    {
    //        public HttpStatusCode StatusCode { get; set; }
    //        public object Data { get; set; }
    //        public TokenData TokenData { get; set; }
    //        public string ErrorCode { get; set; }
    //        public string ReferenceId { get; set; } = "0";
    //        public string BookingReferenceId { get; set; } = "0";

    //        public static BookingResult Success(dynamic res, TokenData token) => new BookingResult
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Data = res,
    //            TokenData = token,
    //            ErrorCode = XConnect.Models.ErrorCode.NoError,
    //            ReferenceId = res.referenceId,
    //            BookingReferenceId = res.bookingReferenceId
    //        };

    //        public static BookingResult Duplicate(dynamic res, TokenData token) => new BookingResult
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Data = res,
    //            TokenData = token,
    //            ErrorCode = XConnect.Models.ErrorCode.DUPLICATION,
    //            ReferenceId = res.referenceId,
    //            BookingReferenceId = res.bookingReferenceId
    //        };

    //        public static BookingResult Error(dynamic res, TokenData token) => new BookingResult
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Data = res,
    //            TokenData = token,
    //            ErrorCode = XConnect.Models.ErrorCode.SomeError,
    //            ReferenceId = res.referenceId,
    //            BookingReferenceId = res.bookingReferenceId
    //        };

    //        public static BookingResult Failed(TokenData token) => new BookingResult
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Data = "Booking Failed",
    //            TokenData = token,
    //            ErrorCode = XConnect.Models.ErrorCode.SomeError
    //        };

    //        public static BookingResult Unauthorized(TokenData token) => new BookingResult
    //        {
    //            StatusCode = HttpStatusCode.Unauthorized,
    //            Data = "Unauthorized",
    //            TokenData = token,
    //            ErrorCode = XConnect.Models.ErrorCode.AutorizationError
    //        };
    //    }

    //    public class AgodaBookingRS
    //    {
    //        public string responseStatus { get; set; }
    //        public string version { get; set; }
    //        public string referenceId { get; set; }
    //        public string bookingReferenceId { get; set; }
    //        public string status { get; set; }
    //        public string remark { get; set; }
    //    }

    //    public class CommoXMLOutPreCheckRequest
    //    {
    //        public class AdvancedOptions
    //        {
    //            public string Currency { get; set; }
    //            public bool RefreshCache { get; set; }
    //            public string CustomerIpAddress { get; set; }
    //            public bool SearchByOneID { get; set; }
    //        }

    //        public class HotelOption
    //        {
    //            public string HotelOptionId { get; set; }
    //            public List<HotelRoom> HotelRooms { get; set; }
    //        }

    //        public class HotelRoom
    //        {
    //            public string RoomNo { get; set; }
    //            public string RoomToken { get; set; }
    //        }

    //        public class Request
    //        {
    //            public string SearchKey { get; set; }
    //            public HotelOption HotelOption { get; set; }
    //        }

    //        public class PreCheckRequest
    //        {
    //            public string Token { get; set; }
    //            public Request Request { get; set; }
    //            public AdvancedOptions AdvancedOptions { get; set; }
    //            public int IsMobile { get; set; }
    //        }


    //    }
    //    public class CommoXMLOutPreCheckResponse
    //    {
    //        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    //        public class HotelOption
    //        {
    //            public string HotelName { get; set; }
    //            public string Nationality { get; set; }
    //            public string BookingToken { get; set; }
    //            public string Status { get; set; }
    //            public int IsPriceChange { get; set; }
    //            public string CheckInDate { get; set; }
    //            public string CheckOutDate { get; set; }
    //            public double TotalPrice { get; set; }
    //            public List<HotelRoom> HotelRooms { get; set; }
    //        }

    //        public class HotelRoom
    //        {
    //            public int UniqueId { get; set; }
    //            public string RoomNo { get; set; }
    //            public string RoomTypeName { get; set; }
    //            public string MealName { get; set; }
    //            public string MappedMealName { get; set; }
    //            public double Price { get; set; }
    //            public PriceChange PriceChange { get; set; }
    //        }

    //        public class PreBookRS
    //        {
    //            public HotelOption HotelOption { get; set; }
    //            public string Currency { get; set; }
    //        }

    //        public class PriceChange
    //        {
    //            public double OldPrice { get; set; }
    //            public double NewPrice { get; set; }
    //        }

    //        public class PreCheckResponse
    //        {
    //            public List<object> Error { get; set; }
    //            public PreBookRS PreBookRS { get; set; }
    //        }


    //    }

    //    public class Getrateplan
    //    {
    //        public class Root
    //        {
    //            public string token { get; set; }
    //        }
    //    }

    //    public class AgodaCancelRQ
    //    {
    //        public string referenceId { get; set; }

    //        public string token { get; set; }
    //        public TokenData TokenData { get; set; }

    //    }

    //    public class AgodaCancelRS
    //    {
    //        public string responseStatus { get; set; }
    //        public string version { get; set; }

    //        public string referenceId { get; set; }
    //        public double cancellationFees { get; set; }


    //        public AgodaCancelRS()
    //        {
    //            responseStatus = "SUCCESS";
    //            //version = new AgodaVersion().Version;

    //        }
    //    }

       


    //}
}
