
namespace XConnect.Models
{
    public class XMLHotelBookingCommon
    {
        public static double B2CMarkup = 10;
        public static int CutOffDays = 5;
        public static string GET = "GET";



        public static string AOXSearch = "http://162.13.98.237/AvailabilityLive/AvailabilityService.asmx";                  //  "http://31.222.130.118/AvailabilityTest1.1/AvailabilityService.asmx";
        public static string AOXSearchAction = "http://www.globalinnovations.com/availability:GI_OTA_HotelAvailabilityRQ";
        public static string AOXCancelAction = "http://GlobalInnovations/ReservationService:GI_OTA_CancelRQ";
        public static string AOXReserve = "http://162.13.98.237/ReservationLive/GIReservationService.asmx";                 //  "http://31.222.130.118/ReservationTest1.1/GIReservationService.asmx";

        public static string PriceBreakDown = "PRICE_BREAKDOWN_REQUEST";
        public static string BookingValuation = "BOOKING_VALUATION_REQUEST";
        public static string BookingInsert = "BOOKING_INSERT_REQUEST";
        public static string BookingCancellation = "BOOKING_CANCEL_REQUEST";
        public static string VoucherDetails = "VOUCHER_DETAILS_REQUEST";
        public static string BookingStatus = "BOOKING_STATUS_REQUEST";


    }
}
