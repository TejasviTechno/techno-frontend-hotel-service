using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Fitruums
{
    public static class FitruumsInfo
    {
        public static string FitruumsHotel_Action = "/1/PostGet/NonStaticXMLAPI.asmx/Search?";
        public static string FitruumsHotelList = "NonStaticXMLAPI.asmx/Search?";

        //recheck 30-05-25
        public static string FitruumsPreBooking = "https://book.fitruums.com/1/PostGet/Booking.asmx/PreBook?"; //07-Jun-2025 http to https
        public static string FitruumsBooking = "https://book.fitruums.com/1/PostGet/Booking.asmx/Book?"; //07-Jun-2025 http to https
        public static string FitruumsCancelBooking = "https://book.fitruums.com/1/PostGet/Booking.asmx/CancelBooking?"; //07-Jun-2025 http to https
        public static string FitruumsGetBookingDetails = "https://book.fitruums.com/1/PostGet/Booking.asmx/GetBookingInformation?"; //07-Jun-2025 http to https
    }
}
