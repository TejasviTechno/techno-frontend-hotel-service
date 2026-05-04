using System.Data;
using XConnect.Models.Hotel;


namespace XConnect.Markup.Interface
{
    public interface IMarkup
    {
        void getAgetMarkupSetting(string suppliername, string raynaid, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, ref double AgentPercentage, ref double AgentMarkupAmount, ref Boolean blnIsShowSupplier, ref Boolean isPerce, int agentid, int hotelRating, int CityID, string CountryId, string CheckinDate1);
        void GetHotelDataCommon(ref double LowestRate, ref bool willAddOrNot, ref Int64 OwnsystemHotelId, ref Int64 RaynaId, ref Int64 GiataId, ref int Rating, ref DataRow dr, int AgentId, int TariffID, DataSet dsAgentDetails, DataSet dsGiatadata, HotelSearchParameter SearchPara, DataTable dtCountryCityMarkup, double B2CMarkup, int IsParentId, double SubAgentMarkup, bool SearchByHotel, List<Int64> HotelIDS);
        double ProcessHotelRateBreakupPrice_WithMarkup(int Multi, int City, string CountryId, double Price, double B2CMarkup, int IsParentId, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, ref double RateBrackUp);

        void ProcessHotelRoom_WithMarkup(double Factor, string Currency, bool IsPolicyDynamic, bool IsCombineRoom, int Multi, int City, string Country, string checkin, string checkout, ref double RatePerStay, ref DataRow drHotelRoom, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, double B2CMarkup, int IsParentId);
        void ProcessHotelRoom_WithMarkup_nites(double Factor, string Currency, bool IsPolicyDynamic, bool IsCombineRoom, int Multi, int City, string Country, string checkin, string checkout, ref double RatePerStay, ref DataRow drHotelRoom, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, double B2CMarkup, int IsParentId, int SupplierID = 0, Int64 GiataId = 0, DataSet dsAgentDetails = null, DataTable dtVatHotelList = null
 );

        Double ProcessHotelCancelationPrice_WithMarkup(ref double MainAgentChage, int Multiply, int City, string Country, double CancellationAmount, bool isPerce, double B2CMarkup, double AgentPercentage, double AgentMarkupAmount, int IsParentId, double SubAgentMarkup, ref double TotalVat);


        void ProcessHotelRoom_WithMarkup_BookingDotComCommision(double Factor, string Currency, bool IsPolicyDynamic, bool IsCombineRoom, int Multi, int City, string Country, string checkin, string checkout, ref double RatePerStay, ref DataRow drHotelRoom, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, double B2CMarkup, int IsParentId, double BeforeCommisionRoomRateForAgent);

        //Recheck - 270525
        Task<DataTable> GetCommonMarkup(int CountryId, int CityID, int MarkupCategaryID, int AgentID = 0);
        //Added on 01-may-26
        void getAgetMarkupSettingNew_new(string suppliername, string raynaid, DataTable dtSuplierMarkup, DataSet dsMarkupDetails, ref double AgentPercentage, ref double AgentMarkupAmount, ref Boolean blnIsShowSupplier, ref Boolean isPerce, int agentid, int hotelRating, int CityID, string CountryId, string CheckinDate1);

    }
}
