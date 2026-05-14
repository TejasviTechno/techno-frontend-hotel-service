using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static XConnect.Supplier.Agoda.Agoda_NewESS;
using System.Xml.Serialization;

namespace XConnect.Supplier.Dingus
{
    public class DingusClass
    {
        public class DingusRoom
        {
            public int NoofAdults { get; set; }
            public int NoofChild { get; set; }
            public int Child1Age { get; set; }
            public int Child2Age { get; set; }
            public int Child3Age { get; set; }
            public int Child4Age { get; set; }
        }

        public class HotelRoom
        {
            public string OwnsystemHotelId { get; set; }
            public string RoomTypeName { get; set; }
            public string RoomTypeCode { get; set; }
            public string MealId { get; set; }
            public string MealName { get; set; }
            public string BoardType { get; set; }
            public string RoomTypeId { get; set; }
            public int RoomNo { get; set; }
            public string EssentialInformation { get; set; }
            public string SupplierCurrency { get; set; }
            public double FinalSellingPrice { get; set; }
            public DateTime FinalPolicyDate { get; set; }
            public string PromotionCode { get; set; }
            public string PromotionName { get; set; }
            public string NumberOfUnits { get; set; }
        }

        public class Roomlist
        {

            public string Meal { get; set; }
            public double Price { get; set; }

            public int RPH { get; set; }
        }
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.xmlsoap.org/soap/envelope/", IsNullable = false)]
        public partial class Envelope
        {
            [System.Xml.Serialization.XmlIgnore]
            private EnvelopeBody bodyField;

            /// <remarks/>
            public EnvelopeBody Body
            {
                get
                {
                    return this.bodyField;
                }
                set
                {
                    this.bodyField = value;
                }
            }
        }


        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public partial class EnvelopeBody
        {

            private OTA_HotelAvailRS oTA_HotelAvailRSField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.opentravel.org/OTA/2003/05")]
            public OTA_HotelAvailRS OTA_HotelAvailRS
            {
                get
                {
                    return this.oTA_HotelAvailRSField;
                }
                set
                {
                    this.oTA_HotelAvailRSField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.opentravel.org/OTA/2003/05", IsNullable = false)]
        public partial class OTA_HotelAvailRS
        {

            private object successField;

            private OTA_HotelAvailRSRoomStay[] roomStaysField;

            private int versionField;

            /// <remarks/>
            public object Success
            {
                get
                {
                    return this.successField;
                }
                set
                {
                    this.successField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlArrayItemAttribute("RoomStay", IsNullable = false)]
            public OTA_HotelAvailRSRoomStay[] RoomStays
            {
                get
                {
                    return this.roomStaysField;
                }
                set
                {
                    this.roomStaysField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int Version
            {
                get
                {
                    return this.versionField;
                }
                set
                {
                    this.versionField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStay
        {

            private OTA_HotelAvailRSRoomStayRoomType[] roomTypesField;

            private OTA_HotelAvailRSRoomStayRoomRate[] roomRatesField;

            private OTA_HotelAvailRSRoomStayCancelPenalty[] cancelPoliciesField;

            private object depositPaymentsField;

            private OTA_HotelAvailRSRoomStayBasicPropertyInfo basicPropertyInfoField;

            private int roomStayCandidateRPHField;

            /// <remarks/>
            [System.Xml.Serialization.XmlArrayItemAttribute("RoomType", IsNullable = false)]
            public OTA_HotelAvailRSRoomStayRoomType[] RoomTypes
            {
                get
                {
                    return this.roomTypesField;
                }
                set
                {
                    this.roomTypesField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlArrayItemAttribute("RoomRate", IsNullable = false)]
            public OTA_HotelAvailRSRoomStayRoomRate[] RoomRates
            {
                get
                {
                    return this.roomRatesField;
                }
                set
                {
                    this.roomRatesField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlArrayItemAttribute("CancelPenalty", IsNullable = false)]
            public OTA_HotelAvailRSRoomStayCancelPenalty[] CancelPolicies
            {
                get
                {
                    return this.cancelPoliciesField;
                }
                set
                {
                    this.cancelPoliciesField = value;
                }
            }

            /// <remarks/>
            public object DepositPayments
            {
                get
                {
                    return this.depositPaymentsField;
                }
                set
                {
                    this.depositPaymentsField = value;
                }
            }

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayBasicPropertyInfo BasicPropertyInfo
            {
                get
                {
                    return this.basicPropertyInfoField;
                }
                set
                {
                    this.basicPropertyInfoField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int RoomStayCandidateRPH
            {
                get
                {
                    return this.roomStayCandidateRPHField;
                }
                set
                {
                    this.roomStayCandidateRPHField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomTypes
        {

            private OTA_HotelAvailRSRoomStayRoomTypesRoomType roomTypeField;

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayRoomTypesRoomType RoomType
            {
                get
                {
                    return this.roomTypeField;
                }
                set
                {
                    this.roomTypeField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomTypesRoomType
        {

            private OTA_HotelAvailRSRoomStayRoomTypesRoomTypeRoomDescription roomDescriptionField;

            private string roomTypeCodeField;

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayRoomTypesRoomTypeRoomDescription RoomDescription
            {
                get
                {
                    return this.roomDescriptionField;
                }
                set
                {
                    this.roomDescriptionField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string RoomTypeCode
            {
                get
                {
                    return this.roomTypeCodeField;
                }
                set
                {
                    this.roomTypeCodeField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomTypesRoomTypeRoomDescription
        {

            private string imageField;

            private string nameField;

            /// <remarks/>
            public string Image
            {
                get
                {
                    return this.imageField;
                }
                set
                {
                    this.imageField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string Name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomRate
        {

            private OTA_HotelAvailRSRoomStayRoomRateRates ratesField;

            private string roomRateDescriptionField;

            private OTA_HotelAvailRSRoomStayRoomRateTotal totalField;

            private int minimumMarkupField;

            private bool isPriceNetoField;

            private string roomTypeCodeField;

            private string mealPlanCodesField;

            private string mealPlanDescriptionField;

            private int ratePlanIDField;

            private string ratePlanCodeField;

            private string rateTypeCodeField;

            private string promotionCodeField;

            private int numberOfUnitsField;

            private int commissionField;

            private bool availabilityStatusField;

            private string reasonNotAvailableField;

            private string promotionNameField;

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayRoomRateRates Rates
            {
                get
                {
                    return this.ratesField;
                }
                set
                {
                    this.ratesField = value;
                }
            }

            /// <remarks/>
            public string RoomRateDescription
            {
                get
                {
                    return this.roomRateDescriptionField;
                }
                set
                {
                    this.roomRateDescriptionField = value;
                }
            }

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayRoomRateTotal Total
            {
                get
                {
                    return this.totalField;
                }
                set
                {
                    this.totalField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int MinimumMarkup
            {
                get
                {
                    return this.minimumMarkupField;
                }
                set
                {
                    this.minimumMarkupField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public bool IsPriceNeto
            {
                get
                {
                    return this.isPriceNetoField;
                }
                set
                {
                    this.isPriceNetoField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string RoomTypeCode
            {
                get
                {
                    return this.roomTypeCodeField;
                }
                set
                {
                    this.roomTypeCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string MealPlanCodes
            {
                get
                {
                    return this.mealPlanCodesField;
                }
                set
                {
                    this.mealPlanCodesField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string MealPlanDescription
            {
                get
                {
                    return this.mealPlanDescriptionField;
                }
                set
                {
                    this.mealPlanDescriptionField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int RatePlanID
            {
                get
                {
                    return this.ratePlanIDField;
                }
                set
                {
                    this.ratePlanIDField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string RatePlanCode
            {
                get
                {
                    return this.ratePlanCodeField;
                }
                set
                {
                    this.ratePlanCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string RateTypeCode
            {
                get
                {
                    return this.rateTypeCodeField;
                }
                set
                {
                    this.rateTypeCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string PromotionCode
            {
                get
                {
                    return this.promotionCodeField;
                }
                set
                {
                    this.promotionCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int NumberOfUnits
            {
                get
                {
                    return this.numberOfUnitsField;
                }
                set
                {
                    this.numberOfUnitsField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int Commission
            {
                get
                {
                    return this.commissionField;
                }
                set
                {
                    this.commissionField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public bool AvailabilityStatus
            {
                get
                {
                    return this.availabilityStatusField;
                }
                set
                {
                    this.availabilityStatusField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string ReasonNotAvailable
            {
                get
                {
                    return this.reasonNotAvailableField;
                }
                set
                {
                    this.reasonNotAvailableField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string PromotionName
            {
                get
                {
                    return this.promotionNameField;
                }
                set
                {
                    this.promotionNameField = value;
                }
            }
        }

        public partial class OTA_HotelAvailRSRoomStayRoomType
        {
            private string roomTypeCodeField;


            private OTA_HotelAvailRSRoomStayRoomTypeRoomDescription RoomDescriptionField;
            public OTA_HotelAvailRSRoomStayRoomTypeRoomDescription RoomDescription
            {
                get
                {
                    return this.RoomDescriptionField;
                }
                set
                {
                    this.RoomDescriptionField = value;
                }
            }
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string RoomTypeCode
            {
                get
                {
                    return this.roomTypeCodeField;
                }
                set
                {
                    this.roomTypeCodeField = value;
                }
            }

        }
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomTypeRoomDescription
        {

            private string NameField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string Name
            {
                get
                {
                    return this.NameField;
                }
                set
                {
                    this.NameField = value;
                }
            }
        }
        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomRateRates
        {

            private OTA_HotelAvailRSRoomStayRoomRateRatesRate rateField;

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayRoomRateRatesRate Rate
            {
                get
                {
                    return this.rateField;
                }
                set
                {
                    this.rateField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomRateRatesRate
        {

            private OTA_HotelAvailRSRoomStayRoomRateRatesRateBase baseField;

            private string rateDescriptionField;

            private System.DateTime expireDateField;

            private System.DateTime effectiveDateField;

            /// <remarks/>
            public OTA_HotelAvailRSRoomStayRoomRateRatesRateBase Base
            {
                get
                {
                    return this.baseField;
                }
                set
                {
                    this.baseField = value;
                }
            }

            /// <remarks/>
            public string RateDescription
            {
                get
                {
                    return this.rateDescriptionField;
                }
                set
                {
                    this.rateDescriptionField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
            public System.DateTime ExpireDate
            {
                get
                {
                    return this.expireDateField;
                }
                set
                {
                    this.expireDateField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
            public System.DateTime EffectiveDate
            {
                get
                {
                    return this.effectiveDateField;
                }
                set
                {
                    this.effectiveDateField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomRateRatesRateBase
        {

            private string currencyCodeField;

            private decimal amountBeforeTaxField;

            private decimal totalOffersAppliedField;

            private decimal amountAfterTaxField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string CurrencyCode
            {
                get
                {
                    return this.currencyCodeField;
                }
                set
                {
                    this.currencyCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal AmountBeforeTax
            {
                get
                {
                    return this.amountBeforeTaxField;
                }
                set
                {
                    this.amountBeforeTaxField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal TotalOffersApplied
            {
                get
                {
                    return this.totalOffersAppliedField;
                }
                set
                {
                    this.totalOffersAppliedField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal AmountAfterTax
            {
                get
                {
                    return this.amountAfterTaxField;
                }
                set
                {
                    this.amountAfterTaxField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayRoomRateTotal
        {

            private decimal totalOffersAppliedField;

            private string currencyCodeField;

            private decimal amountBeforeTaxField;

            private decimal amountAfterTaxField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal TotalOffersApplied
            {
                get
                {
                    return this.totalOffersAppliedField;
                }
                set
                {
                    this.totalOffersAppliedField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string CurrencyCode
            {
                get
                {
                    return this.currencyCodeField;
                }
                set
                {
                    this.currencyCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal AmountBeforeTax
            {
                get
                {
                    return this.amountBeforeTaxField;
                }
                set
                {
                    this.amountBeforeTaxField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal AmountAfterTax
            {
                get
                {
                    return this.amountAfterTaxField;
                }
                set
                {
                    this.amountAfterTaxField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayCancelPenalty
        {

            private decimal amountPercentField;

            private string penaltyDescriptionField;

            private int ratePlanIDField;

            private bool nonRefundableField;

            private System.DateTime startField;

            /// <remarks/>
            public decimal AmountPercent
            {
                get
                {
                    return this.amountPercentField;
                }
                set
                {
                    this.amountPercentField = value;
                }
            }

            /// <remarks/>
            public string PenaltyDescription
            {
                get
                {
                    return this.penaltyDescriptionField;
                }
                set
                {
                    this.penaltyDescriptionField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public int RatePlanID
            {
                get
                {
                    return this.ratePlanIDField;
                }
                set
                {
                    this.ratePlanIDField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public bool NonRefundable
            {
                get
                {
                    return this.nonRefundableField;
                }
                set
                {
                    this.nonRefundableField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public System.DateTime Start
            {
                get
                {
                    return this.startField;
                }
                set
                {
                    this.startField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.opentravel.org/OTA/2003/05")]
        public partial class OTA_HotelAvailRSRoomStayBasicPropertyInfo
        {

            private string hotelCodeField;

            private string hotelNameField;

            private string hotelCategoryCodeField;

            private string hotelCategoryDescriptionField;

            private string hotelCityCodeField;

            private string chainCodeField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string HotelCode
            {
                get
                {
                    return this.hotelCodeField;
                }
                set
                {
                    this.hotelCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string HotelName
            {
                get
                {
                    return this.hotelNameField;
                }
                set
                {
                    this.hotelNameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string HotelCategoryCode
            {
                get
                {
                    return this.hotelCategoryCodeField;
                }
                set
                {
                    this.hotelCategoryCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string HotelCategoryDescription
            {
                get
                {
                    return this.hotelCategoryDescriptionField;
                }
                set
                {
                    this.hotelCategoryDescriptionField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string HotelCityCode
            {
                get
                {
                    return this.hotelCityCodeField;
                }
                set
                {
                    this.hotelCityCodeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string ChainCode
            {
                get
                {
                    return this.chainCodeField;
                }
                set
                {
                    this.chainCodeField = value;
                }
            }
        }

    }

}
