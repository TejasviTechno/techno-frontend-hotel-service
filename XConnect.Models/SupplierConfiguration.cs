using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class SupplierConfiguration
    {
        public ExpediaConfig Expedia { get; set; }
        public ETGConfig ETG { get; set; }
        public HotelbedsConfig Hotelbeds { get; set; }
        public DidaConfig Dida { get; set; }
        public RestelConfig Restel { get; set; }
        public TechnoConfig Techno { get; set; }
        public LetsFlyConfig LetsFly { get; set; }
        public IWTXConfig IWTX { get; set; }
        public TboConfig TBO { get; set; }
        public JuniperConfig Juniper { get; set; }
        public IBoosyConfig IBoosy { get; set; }
        public GoGlobalConfig GoGlobal { get; set; }
        public OmnibeesConfig Omnibees { get; set; }        
        public MGBedBankConfig MGBedBank { get; set; }
        public QtechConfig Qtech { get; set; }
        public HyperguestConfig Hyperguest { get; set; }
        public WelcomebedsConfig Welcomebeds { get; set; }

        public GRNCConfig GRNC { get; set; }
        public ExelyConfig Exely { get; set; }
    }

    public class ExpediaConfig
    {
        public string EAN_Version { get; set; }
        public string EAN_VersionOnly { get; set; }
        public string EAN_VersionV3 { get; set; }
        public string EAN_VersionOnlyV3 { get; set; }
        public string EAN_HotelList_Url { get; set; }
        public string EAN_B2BV3 { get; set; }
        public string EAN_PKGV3 { get; set; }

        public string EANCurrency { get; set; }
        public string EANLanguage { get; set; }
        public string EAN_Platform_Name { get; set; }

        public int EAN_Rate_Plan_Count_10 { get; set; }
        public int EAN_Rate_Plan_Count_200 { get; set; }
        public int EAN_Rate_Plan_Count_250 { get; set; }
        public int EAN_Rate_Plan_Count_1 { get; set; }

        public string EAN_Partner_Point_Of_Sale { get; set; }
        public string EAN_Partner_Point_Of_Sale_COMP { get; set; }

        public string EAN_Rate_Option_Member { get; set; }
        public string EAN_Rate_Option_Closed_User_Group { get; set; }
        public string EAN_Sales_Environment_Hotel_package { get; set; }
        public string EAN_Sales_Environment_Hotel_Only { get; set; }
        public string EAN_Include_All_Rates { get; set; }
        public string EAN_Sort_Type_Preferred { get; set; }
        public string EAN_Billing_Terms_EAC { get; set; }
        public string EAN_Payment_Terms_0 { get; set; }
        public string EAN_Payment_Terms_2 { get; set; }

        public string creditCardNumber_Xpedia { get; set; }
        public string creditCardExpirationMonth_Xpedia { get; set; }
        public string creditCardExpirationYear_Xpedia { get; set; }
        public string creditCardIdentifier_Xpedia { get; set; }
        public string countryCode_Xpedia { get; set; }
        public string homePhone_Xpedia { get; set; }
        public string WorkPhone_Xpedia { get; set; }
        public string creditCardType_Xpedia { get; set; }
        public string country_code_Xpedia { get; set; }
        public string postalCode_Xpedia { get; set; }
        public string city_Xpedia { get; set; }
        public string firstName_Xpedia { get; set; }
        public string lastName_Xpedia { get; set; }
        public string email_Xpedia { get; set; }
        public string address1_Xpedia { get; set; }
        public string area_code_Xpedia { get; set; }
        public string number_Xpedia { get; set; }
    }

    public class ETGConfig
    {
        public string search_serp_hotels { get; set; }
        public string hotelinfourl { get; set; }
        public string ETG_Mail { get; set; }
        public string ETG_Mobile { get; set; }
        public string Prebook { get; set; }
        public string booking_form { get; set; }
        public string booking_finish { get; set; }
        public string booking_finish_status { get; set; }
        public string search_serp_hp { get; set; }
        public string order_cancel { get; set; }
        

    }
    public class HotelbedsConfig
    {
        public string hotels { get; set; }
        public string  checkrates { get; set; }
      public string bookings { get; set; }
      public string bookings2 { get; set; }
      
    }
    public class DidaConfig
    {
        public string DidaHotel_Action { get; set; }
        public string Dida_preBooking_Action { get; set; }
        public string Dida_Booking_Action { get; set; }
        public string Dida_PreCancel_Action { get; set; }
        public string Dida_CancelConfirm_Action { get; set; }
        public string email { get; set; }
        public string WorkPhone { get; set; }
    }
    public class RestelConfig
    {
        public string Key { get; set; }
        public string codigousu { get; set; }
        public string clausu { get; set; }
        public string afiliacio { get; set; }
        public string secacc { get; set; }
    }
    public class TechnoConfig
    {
        public string WE_DMCHotelSearchURL { get; set; }
        public string WE_DMCHotelSearchWithCancellationURL { get; set; }
        public string WE_DMCCancellationPolicyURL { get; set; }
        public string WE_DMCPreBookURL { get; set; }
        public string WE_DMCBookingURL { get; set; }
        public string WE_DMCCancelBookingURL { get; set; }
        public string WE_DMCCancelChargeURL { get; set; }
        public string WE_DMCBookingDetailURL { get; set; }
    }
    public class LetsFlyConfig
    {
        public string LetsFly_HotelSearch { get; set; }
        public string LetsFly_PrebookURL { get; set; }
        public string LetsFly_BookingURL { get; set; }
        public string LetsFly_BookingInfoURL { get; set; }
        public string LetsFly_BookingCancelURL { get; set; }

        public string LetsFly_contactname { get; set; }
        public string LetsFly_contactemail { get; set; }
        public string LetsFly_contactphone { get; set; }
    }
    public class IWTXConfig
    {
        public string IWTX_HotelSearch { get; set; }
        public string IWTXUrl { get; set; }
        public string IWTX_PriceCheck { get; set; }
        public string IWTX_Booking { get; set; }
        public string IWTX_BookingCancel { get; set; }
    }
    public class TboConfig
    {
        public string V1Search { get; set; }
        public string HotelSearchRoomsAction { get; set; }
        public string AvailabilityAndPricingAction { get; set; }
        public string V1PreBook { get; set; }
        public string V1BookUrl { get; set; }
        public string V1BookingDetail { get; set; }
        public string V1Cancel { get; set; }
    }
    public class JuniperConfig
    {
        public string HotelAvail { get; set; }
        public string CheckAvail { get; set; }
        public string Booking { get; set; }

    }
    public class IBoosyConfig
    {
        public string Metha_URL { get; set; }
        public string RS_NoCount { get; set; }
        public string RS_Success { get; set; }
        public int IBoosy_Rate_Plan_Count_10 { get; set; }
        public string Methabook_BookStatus { get; set; }
        public string Bookingurl { get; set; }
        public string Cancel { get; set; }
        public string PriceCheck { get; set; }
    }
    public class GoGlobalConfig
    {
        public string AvailabilitySearch { get; set; }
        public string PriceBreakDown { get; set; }
        public string BookingValuation { get; set; }
        public string BookingInsert { get; set; }
        public string BookingCancellation { get; set; }
        public string VoucherDetails { get; set; }
        public string BookingStatus { get; set; }
    }
    public class OmnibeesConfig
    {
        public string Target { get; set; }
        public string OmnibessHotelAvai { get; set; }
        public string OmnibessHotelBook { get; set; }
        public string DerbySoftAuthorization { get; set; }
        public string OmnibeesCancel { get; set; }
        public string Version { get; set; }
    }
    
    public class MGBedBankConfig
    {
        public string WebSiteURL { get; set; }
        public string WebSiteURLforAPI { get; set; }
        public string Searchs { get; set; }
        public string Currency { get; set; }

        public string RecheckRate { get; set; }
        public string BookHotel { get; set; }
        public string CancelBooking { get; set; }
        public string GetReservationDetail { get; set; }
        public string GetReservationList { get; set; }
        public string GetHotelDetail { get; set; }
        public string GetNationalities { get; set; }
        public string GetDestinationLists { get; set; }
        public string GetPromotionTypes { get; set; }
        public string GetMealPlanLists { get; set; }
    }

    public class QtechConfig
    {
        
        public string HotelAvail { get; set; }
        public string ElevateCurrency { get; set; }
        
        public string Mercan_hotelListAction { get; set; } //= "hotel_search";
        public string BookURL { get; set; }
        public string Mercan_BookingAction { get; set; }
        public string Mercan_CancelAction { get; set; }
        public string get_cancellation_charges { get; set; }
        public string Mercan_CancelltionPolicyAction { get; set; }        
        public string Nirvana_url { get; set; }
        public string CancelltionPolicyAction { get; set; }

    }
    public class HyperguestConfig
    {
        public string HyperguestBookingURL { get; set; }
        public string Prebook_book { get; set; }
    }

    public class WelcomebedsConfig
    {
        public string Welcomebeds_HotelSearch { get; set; }
        public string Welcomebeds_PreBookingURL { get; set; }
        public string Welcomebeds_BookingURL { get; set; }
        public string Welcomebeds_CancelURL { get; set; }

    }

    public class GRNCConfig
    {
        public string HotelSearch { get; set; }
        public string Policies { get; set; }
      
    }
    public class ExelyConfig
    {
        public string BaseUrl { get; set; }
        public string SearchRoomStays { get; set; }
        public string VerifyBooking { get; set; }
        public string CreateBooking { get; set; }
        public string GetBooking { get; set; }
        public string CalculateCancellationPenalty { get; set; }
        public string CancelBooking { get; set; }
        public string GetCancellationRulesByProperty { get; set; }
        public string GetRoomStaysByProperty { get; set; }
        public string? PropertyContent { get; set; }
        public string? PropertyContentByProperty { get; set; }
    }
}
