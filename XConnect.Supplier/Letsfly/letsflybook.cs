using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class letsflybook
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Customerinfo
        {
            public string firstname_zh
            {
                get; set;
            }
            public string gender
            {
                get; set;
            }
            public string birthday
            {
                get; set;
            }
            public string firstname
            {
                get; set;
            }
            public string lastname_zh
            {
                get; set;
            }
            public string lastname
            {
                get; set;
            }
            public string national
            {
                get; set;
            }
            public string telphone
            {
                get; set;
            }
            public string email
            {
                get; set;
            }
        }

        public class Orderinfo
        {
            public int ordercreatetime
            {
                get; set;
            }
            public string city
            {
                get; set;
            }
            public double totalprice
            {
                get; set;
            }
            public string contactphone
            {
                get; set;
            }
            public int adults
            {
                get; set;
            }
            public string lforderno
            {
                get; set;
            }
            public string childrenages
            {
                get; set;
            }
            public string hotelid
            {
                get; set;
            }
            public int roomcount
            {
                get; set;
            }
            public string roomname
            {
                get; set;
            }
            public string currency
            {
                get; set;
            }
            public List<Customerinfo> customerinfo
            {
                get; set;
            }
            public string checkin
            {
                get; set;
            }
            public int orderstatus
            {
                get; set;
            }
            public string contactname
            {
                get; set;
            }
            public string country
            {
                get; set;
            }
            public string roomtypeid
            {
                get; set;
            }
            public string orderno
            {
                get; set;
            }
            public string contactemail
            {
                get; set;
            }
            public string checkout
            {
                get; set;
            }
            public int children
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
