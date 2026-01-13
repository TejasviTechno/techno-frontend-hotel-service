using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Letsfly
{
    public class letsflybookingcancelRQ
    {
        public class Root
        {
            public string orderno { get; set; }
            public string lforderno { get; set; }
        }
    }
}
