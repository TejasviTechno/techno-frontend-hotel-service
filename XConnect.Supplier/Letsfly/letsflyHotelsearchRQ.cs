using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class letsflyHotelsearchRQ
    {
        public class Roomlist
        {
            public int roomnum { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public List<int> childrenages { get; set; }
        }

        public class Root
        {
            public List<string> hotelid { get; set; }
            public string checkin { get; set; }
            public string checkout { get; set; }
            public int roomcount { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public string childrenages { get; set; }
            public string is_precache { get; set; }
            public string nationality { get; set; }
            public int timeout { get; set; }
            public string currency { get; set; }
            public List<Roomlist> roomlist { get; set; }
        }
    }
}
