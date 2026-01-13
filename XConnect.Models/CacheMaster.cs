
namespace XConnect.Models
{
    public static class CacheMaster
    {
        public static string CacheKey_Search = "SearchKeyList_";
        public static string WebConfigKey_Search = "xconnectCache";
        public static string webConfig_CacheTimeout = "CacheTimeOut";
        public static string CacheKey_Extension = ".json";
        public static int CacheKey_Devider = 80;
        public static int RoomLimit = 5;
        public static int RoomLimit_10 = 9;
        public static int RoomLimit_20 = 19;


        public static int CutOffDays = -1;

        public enum _CacheOperation
        {
            read = 0,
            save = 1,
            delete = 3,
            update = 4
        }

        public enum _CacheError
        {
            error = 0,
            success = 1,
            expired = 3,
            notfound = 4
        }

        #region Recheck - 270525
        public enum _cacheHotelOption
        {
            giataSeq = 0,
            ownhotelid = 1,
            supplierId = 2,
            hotelSeq = 3,
        }

        public enum _cacheRoomToken
        {

            Giataid = 0,
            HotelId = 1,
            RoomSeq = 2,
            RoomNo = 3,
        }
        #endregion
    }
}
