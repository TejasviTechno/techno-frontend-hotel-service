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
        public RestelConfig Restel{ get; set; }
        public HotelbedsConfig Hotelbeds { get; set; }
        public DidaConfig Dida { get; set; }
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

    public class HotelbedsConfig
    {
        // Currently empty, add properties as needed
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
    public class ETGConfig
    {
        public string search_serp_hotels { get; set; }
        public string hotelinfourl { get; set; }
        public string ETG_Mail { get; set; }
        public string ETG_Mobile { get; set; }
        public string Prebook { get; set; }
    }
    public class RestelConfig
    {
        public string Key { get; set; }
    }
}
