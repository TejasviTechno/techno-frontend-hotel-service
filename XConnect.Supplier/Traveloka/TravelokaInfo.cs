using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Traveloka
{
    public static class TravelokaInfo
    {
        public static string Tarveloka_HotelList = "accom/v1/list";
        public static string Tarveloka_HotelBulk = "accom/v1/bulk";
        public static string TravelokaBookingURL = "https://api-afiba.afi.traveloka.com/";
        public static string TravelokaAuthURL = "https://api-afiauth.afi.traveloka.com/";
        public static string TravelokaKeyPath = "C:\\WEB\\static.withinearth.com\\XConnectAPI\\cert.p12";
        // public static string TravelokaKeyPath = "D:\\WEB\\LIVE\\liveconnect.withinearth.com\\XConnectAPI\\XConnectAPI\\Hotel\\cert.p12";

        public static string ProductionKey = "client_id=48raop9ambp5tj80elbke1g7ee&client_secret=1tfrl0cj4psb1vmep69demurvcm0qm7h2212et3eo6ncq62ld8hv";
        public static string StagingKey = "client_id=4fujci9ab8jjevae0b65t9fs0h&client_secret=1geuq8avure9bhma1gu0mdaqfobcrk1fts8fr27hbueleto467j4";

    }
}
