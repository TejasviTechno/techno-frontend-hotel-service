using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class letsflysearchRS
    {
        public class Bed
        {
            public int count { get; set; }
            public string desc { get; set; }
            public string code { get; set; }
            public string bedsize { get; set; }
            public int id { get; set; }
        }

        public class Cancellation
        {
            public string cancellationType { get; set; }
            public string cancellationWindowDate { get; set; }
        }

        public class Hotellist
        {
            public string city { get; set; }
            public string country { get; set; }
            public string hotelid { get; set; }
            public string hotelname { get; set; }
            public List<Room> rooms { get; set; }
        }

        public class Meals
        {
            public bool breakfastInclude { get; set; }
        }
        public class ExtraFee
        {
            public bool is_included { get; set; }
            public string currency { get; set; }
            public double amount { get; set; }
            public string type { get; set; }
        }

        public class Room
        {
            public double tax { get; set; }
            public string currency { get; set; }
            public string id { get; set; }
            public int maxChildren { get; set; }
            public double totalprice { get; set; }
            public int freeBroadband { get; set; }
            public List<ExtraFee> extra_fees { get; set; }
            public double fare { get; set; }
            public double price { get; set; }
            public string remarks { get; set; }
            public Meals meals { get; set; }
            public string name { get; set; }
            public int maxOccupancy { get; set; }
            public List<Bed> beds { get; set; }
            public Cancellation cancellation { get; set; }
            public int remainrooms { get; set; }
        }

        public class Root
        {
            public int status { get; set; }
            public string msg { get; set; }
            public List<string> nopricehotellist { get; set; }
            public List<Hotellist> hotellist { get; set; }
            public string sessionid { get; set; }
        }
    }
}
