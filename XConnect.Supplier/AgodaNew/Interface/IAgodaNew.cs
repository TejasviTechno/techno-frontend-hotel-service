using System.Data;
using XConnect.Models.Hotel;
using XConnect.Models;
using System.Diagnostics;
//using static XConnect.Supplier.AgodaNew.AgodaNew;

namespace XConnect.Supplier.AgodaNew.Interface
{
    public interface IAgodaNew
    {
        Task<List<Options>> XMLOut_HotelSearchWithRooms(string requestName, HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, DataSet DTChainCode,DataSet DTCountryblock);
        Task<List<Options>> XMLOut_HotelPreCheck(HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, string ratePlanCode);
        // Task<BookingResult> AgodaBookingProcess(AgodaBookingRQ rq, TokenData tokenData, CommonRequest commonRequest, string headerToken);
        AgodaBookingRS XMLOutBookingProcess(AgodaBookingRQ agodaRQ, TokenData tokenData, Stopwatch Timer, string rateplanstring);

    }
}
