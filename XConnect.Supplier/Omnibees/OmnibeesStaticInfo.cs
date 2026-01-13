using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Omnibees
{
    public static class OmnibeesStaticInfo
    {
        public static int Target = 1;
        public static string OmnibessHotelAvai = "/GetHotelAvail";
        public static string OmnibessHotelBook = "/SendHotelRes";
        public static string DerbySoftAuthorization = "Bearer 971cec3d3d6642c6b034a70391e62e10";
        public static string OmnibeesCancel = "/SendHotelResCancel";
    }
}
