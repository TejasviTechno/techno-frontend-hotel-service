using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.GoGlobal
{
    public class GoGlobal
    {
        public GoGlobal()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public class Stats
        {
            public int HotelQty { get; set; }
            public int ResultsQty { get; set; }
        }

        public class Header
        {
            public string Agency { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string Operation { get; set; }
            public string OperationType { get; set; }
            public Stats Stats { get; set; }
        }

        public class Offer
        {
            public string HotelSearchCode { get; set; }
            public string CxlDeadLine { get; set; }
            public bool NonRef { get; set; }
            public List<string> Rooms { get; set; }
            public string RoomBasis { get; set; }
            public int Availability { get; set; }
            public int TotalPrice { get; set; }
            public string Currency { get; set; }
            public string Category { get; set; }
            public string Remark { get; set; }
            public string Special { get; set; }
            public bool Preferred { get; set; }
        }

        public class Hotel
        {
            public string HotelName { get; set; }
            public int HotelCode { get; set; }
            public int CountryId { get; set; }
            public int CityId { get; set; }
            public string Location { get; set; }
            public string Thumbnail { get; set; }
            public double Longitude { get; set; }
            public double Latitude { get; set; }
            public List<Offer> Offers { get; set; }
        }

        public class GoGlobalHotel
        {
            public Header Header { get; set; }
            public List<Hotel> Hotels { get; set; }
        }

    }
}
