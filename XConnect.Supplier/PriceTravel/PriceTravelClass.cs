using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.PriceTravel
{
    public class PriceTravelClass
    {
    }

    #region Pricetravel class
    public class CancellationPolicy_PT
    {
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public double amount { get; set; }
        public string currency { get; set; }
    }

    public class Detail
    {
        public string type { get; set; }
        public double amount { get; set; }
    }

    public class FeesNotIncluded
    {
        public double total { get; set; }
        public List<Detail> details { get; set; }
        public string currency { get; set; }
    }

    public class Hotel_PriceTravel
    {
        public List<Room_PT> rooms { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }

    public class Rate
    {
        public string rateKey { get; set; }
        public string clientCurrency { get; set; }
        public List<CancellationPolicy_PT> cancellationPolicies { get; set; }
        public List<ReservationPolicy> reservationPolicies { get; set; }
        public double amount { get; set; }
        public double publicAmount { get; set; }
        public Taxes taxes { get; set; }
        public FeesNotIncluded feesNotIncluded { get; set; }
    }

    public class ReservationPolicy
    {
        public string type { get; set; }
        public string code { get; set; }
        public string description { get; set; }
    }

    public class Room_PT
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<Rate> rates { get; set; }
    }

    public class Root_PriceTravel
    {
        public object hotel { get; set; }
        public List<Hotel_PriceTravel> hotels { get; set; }
        public string eventId { get; set; }
        public object error { get; set; }
    }

    public class Taxes
    {
        public double total { get; set; }
        public List<Detail> details { get; set; }
    }


    #endregion

    public class PriceTravelBookingIds
    {
        public string bookingKey { get; set; }
        public string eventId { get; set; }
        public object error { get; set; }
    }
}
