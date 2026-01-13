using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Dida
{
    public static class DidaInfo
    {
        public static string DidaHotel_Action = "/api/rate/pricesearch?$format=json";
        public static string Dida_preBooking_Action = "/api/rate/PriceConfirm?$format=json";
        public static string Dida_Booking_Action = "/api/booking/HotelBookingConfirm?$format=json";
        public static string Dida_PreCancel_Action = "/api/booking/HotelBookingCancel?$format=json";
        public static string Dida_CancelConfirm_Action = "/api/booking/HotelBookingCancelConfirm?$format=json";

        public static string email = "ops@withinearth.com";
        public static string WorkPhone = " +603-2166-3969";
    }
}
