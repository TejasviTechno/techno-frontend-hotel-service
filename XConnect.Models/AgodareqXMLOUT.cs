using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace XConnect.Models
{
    public class AgodareqXMLOUT
    {
        #region "Available Request Class"
        public class AdvancedOptions
        {
            public string Currency { get; set; }
            public bool RefreshCache { get; set; }
            public string CustomerIpAddress { get; set; }
            public bool SearchByOneID { get; set; }
        }

        public class Filters
        {
            public int IsRecommendedOnly { get; set; }
            public int IsShowRooms { get; set; }
            public int IsOnlyAvailable { get; set; }
            public StarRating StarRating { get; set; }
            public string HotelIDs { get; set; }
        }

        public class Request
        {
            public List<Room> Rooms { get; set; }
            public int CityID { get; set; }
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public int NoofNights { get; set; }
            public string Nationality { get; set; }
            public Filters Filters { get; set; }
        }

        public class Room
        {
            public int RoomNo { get; set; }
            public int NoofAdults { get; set; }
            public int NoOfChild { get; set; }
            public List<int> ChildAge { get; set; }
        }

        public class AvailabilityRequest
        {
            public string Token { get; set; }
            public Request Request { get; set; }
            public AdvancedOptions AdvancedOptions { get; set; }
            public int IsMobile { get; set; }
        }

        public class StarRating
        {
            public int Min { get; set; }
            public int Max { get; set; }
        }
        #endregion        
    }
    public class CommoXMLOutResponse
    {
        #region "Available Response Class"
        public class AvailabilityResponse
        {
            public List<ErrorDetail> Error { get; set; }
            public AvailabilityRS AvailabilityRS { get; set; } = null;
        }
        public class ErrorDetail  // New class for error objects
        {
            public string type { get; set; }
            public string code { get; set; }
            public string description { get; set; }
        }
        public class AvailabilityRS
        {
            public string SearchKey { get; set; }
            public int Count { get; set; }
            public string Currency { get; set; }
            public List<HotelResult> HotelResult { get; set; }
        }

        public class HotelResult
        {
            public double StartPrice { get; set; }
            public int HotelId { get; set; }
            public List<HotelOption> HotelOption { get; set; }
        }

        public class HotelOption
        {
            public string HotelOptionId { get; set; }
            public double MinPrice { get; set; }
            public bool IsCombineRoom { get; set; }
            public List<List<Room>> HotelRooms { get; set; }
        }

        public class Room
        {
            public string RoomNo { get; set; }
            public string RoomTypeName { get; set; }
            public string MealName { get; set; }
            public string MappedMealName { get; set; }

            public double Price { get; set; }
            public string BookingStatus { get; set; }
            public string RoomToken { get; set; }
            public string RoomId { get; set; }

            public List<CancellationPolicy> CancellationPolicy { get; set; }
        }

        public class CancellationPolicy
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public double CancellationPrice { get; set; }
        }
        #endregion
    }

    public class CommoXMLOutPreCheckRequest
    {
        public class AdvancedOptions
        {
            public string Currency { get; set; }
            public bool RefreshCache { get; set; }
            public string CustomerIpAddress { get; set; }
            public bool SearchByOneID { get; set; }
        }

        public class HotelOption
        {
            public string HotelOptionId { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public string RoomNo { get; set; }
            public string RoomToken { get; set; }
        }

        public class Request
        {
            public string SearchKey { get; set; }
            public HotelOption HotelOption { get; set; }
        }

        public class PreCheckRequest
        {
            public string Token { get; set; }
            public Request Request { get; set; }
            public AdvancedOptions AdvancedOptions { get; set; }
            public int IsMobile { get; set; }
        }


    }

    public class CommoXMLOutPreCheckResponse
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class HotelOption
        {
            public string HotelName { get; set; }
            public string Nationality { get; set; }
            public string BookingToken { get; set; }
            public string Status { get; set; }
            public int IsPriceChange { get; set; }
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public double TotalPrice { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public int UniqueId { get; set; }
            public string RoomNo { get; set; }
            public string RoomTypeName { get; set; }
            public string MealName { get; set; }
            public string MappedMealName { get; set; }
            public double Price { get; set; }
            public PriceChange PriceChange { get; set; }
        }

        public class PreBookRS
        {
            public HotelOption HotelOption { get; set; }
            public string Currency { get; set; }
        }

        public class PriceChange
        {
            public double OldPrice { get; set; }
            public double NewPrice { get; set; }
        }

        public class PreCheckResponse
        {
            public List<object> Error { get; set; }
            public PreBookRS PreBookRS { get; set; }
        }


    }

    public class CommonXMLBookRequest
    {
        public class AdvancedOptions
        {
            public string Currency { get; set; }
            public string CustomerIpAddress { get; set; }
        }

        public class BookRQ
        {
            public string BookingToken { get; set; }
            public decimal TotalPrice { get; set; }
            public string InternalReference { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public int UniqueId { get; set; }
            public int RoomNo { get; set; }
            public int IsLead { get; set; }
            public string PaxType { get; set; }
            public string Prefix { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int ChildAge { get; set; }
        }

        public class Request
        {
            public string SearchKey { get; set; }
            public BookRQ BookRQ { get; set; }
        }

        public class BookingRequest
        {
            public string Token { get; set; }
            public Request Request { get; set; }
            public AdvancedOptions AdvancedOptions { get; set; }


        }
    }
    public class XMLOutRoomDetail
    {
        public int RoomNo { get; set; }
        public int NoofPassanger { get; set; }
        //public int ChildAge { get; set; }
        public List<int> ChildAge { get; set; }
    }

    public class CommonXMLOutBookingDetailRequest
    {
        public class AdvancedOptions
        {
            public string Currency { get; set; }
            public bool RefreshCache { get; set; }
            public string CustomerIpAddress { get; set; }
            public bool SearchByOneID { get; set; }
        }

        public class BookingDetailRQ
        {
            public int BookingId { get; set; }
            public string InternalReference { get; set; }
            public string ReferenceNo { get; set; }
        }

        public class Request
        {
            public BookingDetailRQ BookingDetailRQ { get; set; }
        }

        public class BookingDetailRequest
        {
            public string Token { get; set; }
            public Request Request { get; set; }
            public AdvancedOptions AdvancedOptions { get; set; }
        }
    }
    public class CommonXMLOutBookingDetailResponse
    {
        public class BookingDetailRS
        {
            public HotelOption HotelOption { get; set; }
            public string Currency { get; set; }
        }

        public class HotelOption
        {
            public string HotelName { get; set; }
            public string HotelId { get; set; }
            public string Nationality { get; set; }
            public int BookingId { get; set; }
            public string InternalReference { get; set; }
            public string ReferenceNo { get; set; }
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public string BookingDate { get; set; }
            public string LeadPaxName { get; set; }
            public double TotalPrice { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public string RoomNo { get; set; }
            public string RoomTypeName { get; set; }
            public string MealName { get; set; }
            public string MappedMealName { get; set; }
            public double Price { get; set; }
            public string BookingStatus { get; set; }
            public List<object> CancellationPolicy { get; set; }
            public List<Passenger> Passenger { get; set; }
        }

        public class Passenger
        {
            public int IsLead { get; set; }
            public string PaxType { get; set; }
            public string Name { get; set; }
            public string ChildAge { get; set; }
        }

        public class BookingDetailResponse
        {
            public List<_Errorbook> Error { get; set; }
            public BookingDetailRS BookingDetailRS { get; set; }
        }
        public class _Errorbook
        {
            public string type { get; set; }
            public string code { get; set; }
            public string description { get; set; }
        }

    }

    public class CommonXMLOutCheckHotelCancellationChargesRequest
    {
        public class AdvancedOptions
        {
            public string Currency { get; set; }
        }

        public class CheckHotelCancellationChargesRQ
        {
            public int BookingId { get; set; }
            public string InternalReference { get; set; }
            public string ReferenceNo { get; set; }
        }

        public class Request
        {
            public CheckHotelCancellationChargesRQ CheckHotelCancellationChargesRQ { get; set; }
        }

        public class CheckHotelCancellationChargesRequest
        {
            public string Token { get; set; }
            public Request Request { get; set; }
            public AdvancedOptions AdvancedOptions { get; set; }
        }
    }
    public class CommonXMLOutCheckHotelCancellationChargesResponse
    {
        public class CheckHotelCancellationChargesRS
        {
            public HotelOption HotelOption { get; set; }
            public string Currency { get; set; }
        }

        public class HotelOption
        {
            public int BookingId { get; set; }
            public double TotalPrice { get; set; }
            public double TotalCharge { get; set; }
            public double TotalRefund { get; set; }
            public string CancelCode { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public int BookingDetailId { get; set; }
            public string RoomTypeName { get; set; }
            public double Price { get; set; }
            public double RefundAmount { get; set; }
        }

        public class CheckHotelCancellationChargesResponse
        {
            public List<ErrorDetail> Error { get; set; }
            public CheckHotelCancellationChargesRS CheckHotelCancellationChargesRS { get; set; }
        }
        public class ErrorDetail  // New class for error objects
        {
            public string type { get; set; }
            public string code { get; set; }
            public string description { get; set; }
        }
    }
    public class CommonXMLOutCancelBookingRequest
    {
        public class AdvancedOptions
        {
            public string Currency { get; set; }
        }
        public class CancelRQ
        {
            public int BookingId { get; set; }
            public int BookingDetailId { get; set; }

            public string CancelCode { get; set; }
            public int CancelAll { get; set; }
            public string Reason { get; set; }

            
        }
        public class Request
        {
            public CancelRQ CancelRQ { get; set; }
        }
        public class CancelBookingRequest
        {
            public string Token { get; set; }
            public Request Request { get; set; }
            public AdvancedOptions AdvancedOptions { get; set; }
        }
    }
    public class CommonXMLOutCancelBookingResponse
    {
        public class CancelRS
        {
            public HotelOption HotelOption { get; set; }
            public string Currency { get; set; }
        }

        public class HotelOption
        {
            public int BookingId { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public string RoomTypeName { get; set; }
            public string Message { get; set; }
            public int CancelStatus { get; set; }
        }

        public class CancelBookingResponse
        {
            public List<object> Error { get; set; }
            public CancelRS CancelRS { get; set; }
        }

        public class AgodaCancelRQ
        {
            public string referenceId { get; set; }

            public string token { get; set; }
            public TokenData TokenData { get; set; }

        }
    }
}
