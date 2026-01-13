using System.Data;
using XConnect.Models;
using XConnect.Models.Hotel;


namespace XConnect.CommonUtilities.Interface
{
    public interface ISupplierRes
    {
        //Task<Options> GetCommonRoomForSuppliers(Int64 GiataId, TokenData tokenData, AdvancedOptions advancedOptions, double LowestRate, DataRow drXmlHotel, DataTable dtHotelRoom, DataTable dt_Cancellation, DataTable dt_RateBreackUp, HotelSearchParameter SearchParam); //cmt on 02-JAN-26

        Task<Options> GetCommonRoomForSuppliers(Int64 GiataId, TokenData tokenData, AdvancedOptions advancedOptions, double LowestRate, DataRow drXmlHotel, DataTable dtHotelRoom, DataTable dt_Cancellation, DataTable dt_RateBreackUp, HotelSearchParameter SearchParam, bool IsRoomRestriction = false, int NoOfReturnRoom = 0, int ForMoreThenRooms = 1);//Added on 02-JAN-26


        bool SwapMealName(string AgentCode);

    }
}
