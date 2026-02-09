using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class RemoteServices
    {
        public CommonServices CommonServices { get; set; }
    }

    public class CommonServices
    {
        public string BaseUrl { get; set; }
        public string ServiceName { get; set; }
    }
}
