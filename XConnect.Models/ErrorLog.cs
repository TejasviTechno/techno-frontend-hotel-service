using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public static class ErrorLog
    {
        public enum _LogAPI
        {
            Recheck = 2,
            Prebook = 3,
            Book = 4,
            CancelBooking = 5
        }
    }
}
