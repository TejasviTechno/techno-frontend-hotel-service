using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class RedisSettings
    {
        public string RedisDB_ConnectionStr1 { get; set; }
        //public string MongoDB_ConnectionStr { get; set; }
        public List<string> ClusterNodes { get; set; }
    }
}
