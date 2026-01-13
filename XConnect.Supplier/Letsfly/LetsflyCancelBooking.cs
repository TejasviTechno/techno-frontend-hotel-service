using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class LetsflyCancelBooking
    {
        public class Orderinfo
        {
            public string lforderno
            {
                get; set;
            }
            public string orderno
            {
                get; set;
            }
        }

        public class Root
        {
            public int status
            {
                get; set;
            }
            public string msg
            {
                get; set;
            }
            public string sessionid
            {
                get; set;
            }
            public Orderinfo orderinfo
            {
                get; set;
            }
        }
    }
}
