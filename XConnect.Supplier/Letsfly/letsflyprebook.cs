using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class letsflyprebook
    {

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Bed
        {
            public string count
            {
                get; set;
            }
            public string bedsize
            {
                get; set;
            }
            public string code
            {
                get; set;
            }
            public int id
            {
                get; set;
            }
            public string desc
            {
                get; set;
            }
        }

        public class Cancellation
        {
            public string cancellationType
            {
                get; set;
            }
            public string cancellationWindowDate
            {
                get; set;
            }
        }

        public class Meals
        {
            public bool breakfastInclude
            {
                get; set;
            }
        }

        public class Room
        {
            public double tax
            {
                get; set;
            }
            public string currency
            {
                get; set;
            }
            public string id
            {
                get; set;
            }
            public int freeBroadband
            {
                get; set;
            }
            public List<Bed> beds
            {
                get; set;
            }
            public double totalprice
            {
                get; set;
            }
            public double fare
            {
                get; set;
            }
            public int maxChildren
            {
                get; set;
            }
            public double price
            {
                get; set;
            }
            public string remarks
            {
                get; set;
            }
            public Meals meals
            {
                get; set;
            }
            public string name
            {
                get; set;
            }
            public int maxOccupancy
            {
                get; set;
            }
            public Cancellation cancellation
            {
                get; set;
            }
            public int remainrooms
            {
                get; set;
            }
        }

        public class letsflyprebookresponse
        {
            public int status
            {
                get; set;
            }
            public string city
            {
                get; set;
            }
            public string country
            {
                get; set;
            }
            public string hotelid
            {
                get; set;
            }
            public string sessionid
            {
                get; set;
            }
            public string hotelname
            {
                get; set;
            }
            public List<Room> rooms
            {
                get; set;
            }
            public string msg
            {
                get; set;
            }
        }

    }
}
