using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class letsflybookingRQ
    {
        public class Customerinfo
        {
            public int roomnum { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string nationality { get; set; }
            //public string telephone { get; set; }
            //public string birthday { get; set; }
            //public string gender { get; set; }
            //public string email { get; set; }
        }

        public class Root
        {
            public string hotelid { get; set; }
            public string checkin { get; set; }
            public string checkout { get; set; }
            public int roomcount { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public string childrenages { get; set; }
            public string roomtypeid { get; set; }
            public string sessionid { get; set; }
            public string orderno { get; set; }
            public string roomname { get; set; }
            public double totalprice { get; set; }
            // public string currency { get; set; }
            public string contactname { get; set; }
            public string contactemail { get; set; }
            public string contactphone { get; set; }
            public string special_request { get; set; }
            public List<Customerinfo> customerinfo { get; set; }
        }
    }
}
