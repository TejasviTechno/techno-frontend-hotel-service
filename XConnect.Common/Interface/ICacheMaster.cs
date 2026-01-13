using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;

namespace XConnect.CommonUtilities.Interface
{
    public interface ICacheMaster
    {
        Options LoadHotelfromMainCache_mongodb(ref AvailHotelList finalResponse, string SearchKey, TokenData tokenData, string OwnsystemHotelId, string SupplierID, int giataSequence, int HotelSequence, ref int giataid_Seq, ref int hotelid_Seq, ref int status);//#MongoDB
        int UpdateCacheForHotelListMaster_mongodb(int giataSequence, string filepath, string Extension, string NewSearchKey, TokenData Tokendata, ref AvailHotelList finalResponse, int operation, CacheRoomToken cacheRoomToken);//#MongoDB
    }
}
