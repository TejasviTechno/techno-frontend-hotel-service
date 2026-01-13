using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;

namespace XConnect.CommonUtilities.Interface
{
    public interface ITokenReaderService
    {
        TokenDetails getTokenDetails(string token);
    }
}
