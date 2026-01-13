using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Jumeirah
{
    public class Jumeirah
    {
        public Jumeirah()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

        #region Availability Response Classes

        public class CancellationPolicy
        {
            public string policy_text { get; set; }
            public double penalty_amount { get; set; }
            public string currency_code { get; set; }
            public bool penalty_inclusive_tax { get; set; }
            public string policy_code { get; set; }
            public DateTime absolute_deadline_datetime { get; set; }
            public bool cancellation_allowed { get; set; }
        }

        public class GuaranteePolicy
        {
            public string policy_text { get; set; }
            public double guarantee_amount { get; set; }
            public string currency_code { get; set; }
            public string policy_code { get; set; }
        }

        public class Tax
        {
            public double amount { get; set; }
            public string tax_name { get; set; }
            public int ota_tax_code { get; set; }
            public int charge_frequency { get; set; }
            public string date { get; set; }
        }

        public class NightlyRate
        {
            public string date { get; set; }
            public double fee { get; set; }
            public double price { get; set; }
            public double tax { get; set; }
            public double total { get; set; }
        }

        public class PlanFeature
        {
            public string code { get; set; }
            public string desc { get; set; }
        }

        public class AvailablePlan
        {
            public string rate_plan_code { get; set; }
            public double total_amount_before_tax { get; set; }
            public double total_amount_after_tax { get; set; }
            public double total_tax_amount { get; set; }
            public CancellationPolicy cancellation_policy { get; set; }
            public GuaranteePolicy guarantee_policy { get; set; }
            public List<Tax> taxes { get; set; }
            public List<NightlyRate> nightly_rates { get; set; }
            public string currency_code { get; set; }
            public string rate_plan_name { get; set; }
            public string rate_plan_desc { get; set; }
            public bool promo_code_applied { get; set; }
            public List<PlanFeature> plan_features { get; set; }
            public bool breakfast_included { get; set; }
            public bool lunch_included { get; set; }
            public bool dinner_included { get; set; }
            public int meal_plan_code { get; set; }
            public int no_of_units_available { get; set; }
        }

        public class AvailablityPrice
        {
            public string room_type_code { get; set; }
            public List<AvailablePlan> available_plans { get; set; }
        }

        public class Root
        {
            public bool success { get; set; }
            public List<AvailablityPrice> availablity_price { get; set; }
        }
        #endregion
        #region RoomName class
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Jumeirahroomname
        {

            public class Amenityroomname
            {
                public string ota_code { get; set; }
                public string code_detail { get; set; }
                public string desc { get; set; }
            }

            public class Imageroomname
            {
                public string url { get; set; }
            }

            public class Roomroomname
            {
                public string type_code { get; set; }
                public string type_name { get; set; }
                public int max_occupancy { get; set; }
                public int number_beds { get; set; }
                public int max_rollaways { get; set; }
                public int ota_room_view_code { get; set; }
                public string room_view_name { get; set; }
                public string short_desc { get; set; }
                public string long_desc { get; set; }
                public List<Imageroomname> images { get; set; }
                public List<Amenityroomname> amenities { get; set; }
            }

            public class Rootroomname
            {
                public bool success { get; set; }
                public List<Roomroomname> rooms { get; set; }
            }
        }
        #endregion
        #region Price Check Class
        public class PriceCheck
        {
            public string total_amount_after_tax { get; set; }
            public string room_type_code { get; set; }
            public string total_tax_amount { get; set; }
            public string rate_plan_code { get; set; }
            public string promo_code { get; set; }
            public string total_amount_before_tax { get; set; }
            public string offset_deadline_time_unit { get; set; }
            public string offset_deadline_time_unit_multiplier { get; set; }
            public string offset_deadline_time { get; set; }
            public string cancellation_allowed { get; set; }
            public string policy_text { get; set; }
            public string policy_code { get; set; }
            public string penalty_amount { get; set; }
            public string currency_code { get; set; }
            public string penalty_inclusive_tax { get; set; }
        }

        public class PriceCheckRoot
        {
            public bool success { get; set; }
            public PriceCheck price_check { get; set; }
        }

        #endregion
        #region Book Classes
        #region Book Request
        public class ReqRoom
        {
            public string room_type_code { get; set; }
            public string rate_plan_code { get; set; }
            public string promo_code { get; set; }
            public string adults_count { get; set; }
            public string children_count { get; set; }
            public string infants_count { get; set; }
            public string special_request { get; set; }
        }

        public class RootBook
        {
            public string phone_no { get; set; }
            public string card_security_code { get; set; }
            public string card_expiry_date { get; set; }
            public string card_number { get; set; }
            public string card_holder_name { get; set; }
            public string first_name { get; set; }
            public string email { get; set; }
            public string partner_code { get; set; }
            public string last_name { get; set; }
            public string hotel_code { get; set; }
            public string card_type { get; set; }
            public string date_check_out { get; set; }
            public string date_check_in { get; set; }
            public List<ReqRoom> req_rooms { get; set; }
        }
        #endregion
        #endregion
        #region Cancel Request
        #region Cancel Request
        public class RootCancel
        {
            public string last_name { get; set; }
            public string email { get; set; }
        }
        #endregion
        #endregion
    }
}
