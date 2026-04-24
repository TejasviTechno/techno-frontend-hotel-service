using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XConnect.Models.Hotel
{
    public class HotelDetails
    {
    }

    public class CacheSearchpara
    {
        public int NoofAdults { get; set; }
        public int NoOfChild { get; set; }
        public int ChildAge1 { get; set; }
        public int ChildAge2 { get; set; }
        public string Nationality { get; set; }
        public string HotelID { get; set; }
        public string checkInDate { get; set; }
        public string checkOutDate { get; set; }
        public int CityID { get; set; }
        public int nights { get; set; }
        public int RefreshCacheHour { get; set; }
    }

    public class HotelSearchParameter
    {
        public HotelSearchParameter()
        {
            Rooms = new List<RoomDetails>();
        }
        public bool PastDate { get; set; }
        public int IsAvailbilityWithCancellation { get; set; }//1=true
        public int IsAvailbilityWithCancellationv2 { get; set; }//1=true // Add On 29-JAN-2025
        public int IsAPI { get; set; }
        public int AgentID { get; set; }
        public string UserName { get; set; } = "";
        public string BookForUserCode { get; set; }
        public string CityName { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int CityID { get; set; }

        [JsonConverter(typeof(StringToIntConverter))]

        public int CountryID { get; set; }

        public string HotelID { get; set; }
        public string HotelName { get; set; } = "";
        public string LocationID { get; set; }
        public string Location { get; set; } = "";
        public string Nationality { get; set; }
        public string CountryCode { get; set; }
        public string Residency { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int NoofNights { get; set; }
        public List<RoomDetails> Rooms { get; set; }
        public List<RoomTypeDetails> RoomsType { get; set; }
        public int AgentMarkup { get; set; }
        public int AgentCompanyPercentage { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int StarStart { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int StarEnd { get; set; }
        public string IsB2BOrB2C { get; set; } = "0";
        public string SelectedStart { get; set; }
        public string CurrentDate { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int IsMobile { get; set; }
        public string AgentCode { get; set; }
        public int IsAvailable { get; set; }
        public int IsForDetails { get; set; }
        public int UserCartId { get; set; }
        public string HotelType { get; set; } = "available";
        public string GiataId { get; set; }
        public string ContractId { get; set; }
        public int PackageId { get; set; }
        public string SuppilerName { get; set; }
        public string IsSearchFor { get; set; } = "multi";
        public int IsPackage { get; set; }
        public string UniqueNo { get; set; }
        public string UserType { get; set; }
        public int IsWebsite { get; set; }
        public bool IsHitOther { get; set; } = false;
        public int RoomNo { get; set; }
        public string MappingId { get; set; }
        public string SupplierTime { get; set; }
        public int hotelRating { get; set; }
        public Filters Filters { get; set; }
        public string SearchKey { get; set; }
        public int City_ID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SuppilerId { get; set; }



        //
        public DateTime ParseCheckInDate { get; set; } 
        public DateTime ParseCheckOutDate { get; set; } 
        public string CheckInDateSubstring { get; set; } 
        public string CheckOutDateSubstring { get; set; } 


    }

    public class RoomDetails
    {
        [JsonConverter(typeof(StringToIntConverter))]

        public int RoomNo { get; set; }
        public string RoomName { get; set; }
        public string RoomType { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int NoofAdults { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int NoofChild { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int Child1Age { get; set; }
        [JsonConverter(typeof(StringToIntConverter))]

        public int Child2Age { get; set; }
        public string Occupancy { get; set; }

        [MaxLength(2)]
        public int[] ChildAge { get; set; }
    }
    public class RoomDetailsV2
    {
        public int RoomNo { get; set; }
        public string RoomName { get; set; }
        public string RoomType { get; set; }
        public int NoofAdults { get; set; }
        public int NoofChild { get; set; }
        public int Child1Age { get; set; }
        public int Child2Age { get; set; }
        public string Occupancy { get; set; }
        public decimal VATAmount { get; set; }
        public int LoyaltyPoints { get; set; }
    }

    public class Filters
    {
        [JsonConverter(typeof(StringToIntConverter))]

        public int IsOnlyAvailable { get; set; } = 1;
        [JsonConverter(typeof(StringToIntConverter))]

        public int IsShowRooms { get; set; } = 0;
        [JsonConverter(typeof(StringToIntConverter))]

        public int IsRecommendedOnly { get; set; } = 0;
        public string HotelIDs { get; set; } = "";
        public StarRatingFilter StarRating { get; set; }
        public bool IsCombineRoom { get; set; }

        public bool SearchByHotel { get; set; }

    }
    public class StarRatingFilter
    {
        [JsonConverter(typeof(StringToIntConverter))]
        public int Min { get; set; } = 0;
        [JsonConverter(typeof(StringToIntConverter))]
        public int Max { get; set; } = 0;

    }

    public class RoomTypeDetails
    {
        public string RoomNo { get; set; }
        public string RoomTypeId { get; set; }
        public string MealId { get; set; }
    }

    #region Prebook
    public class Shopping_CartDataForHotelXML : CancelationPolicyDetailsNew
    {
        public int AgentId { get; set; }
        public int UserCartId { get; set; }
        public int GuestUserId { get; set; }
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ImagePath { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string TourDuration { get; set; }
        public string TourStartTime { get; set; }
        public int NoOfAdults { get; set; }
        public int NoOfChild { get; set; }
        public decimal AdultRate { get; set; }
        public decimal ChildRate { get; set; }
        public decimal BaseAdultRate { get; set; }
        public decimal BaseChildRate { get; set; }
        public string ProductType { get; set; }
        public int BookingStatus { get; set; }
        public string VisaReqFor { get; set; }
        public string Nationality { get; set; }
        public decimal ServiceTotal { get; set; }
        public decimal BaseServiceTotal { get; set; }
        public int ShoppingType { get; set; }
        public int IsGuestUser { get; set; }
        public int OTB { get; set; }
        public DateTime EntryDate { get; set; }
        public string cancellationValue { get; set; }
        public string ProcessingTime { get; set; }
        public string HotelCity { get; set; }
        //  public List<HotelPassengers> HotelPassengers = new List<HotelPassengers>();
        public List<ShoppingCart_DetailsNew> ShoppingCartDetails = new List<ShoppingCart_DetailsNew>();
        public List<_PolicyDetails> PolicyList = new List<_PolicyDetails>();
        public List<ClsRoomRateDetails> RoomRateList = new List<ClsRoomRateDetails>();

        public string CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; }

        public bool IsB2B { get; set; }
        public decimal Percentage { get; set; }

        //Third party hotel related data
        public string XMLHotelId { get; set; }
        public string XMLCityId { get; set; }
        public string XMLCountryId { get; set; }
        public string OfficeId { get; set; }
        public string ContractId { get; set; }

        public string Provider { get; set; }
        public string SupplierName { get; set; }

        public int TourOption { get; set; }
        public bool IsCombo { get; set; }
        public int ComboId { get; set; }

        public string StrCheckInDate { get; set; }
        public string StrCheckOutDate { get; set; }
        public string StrEntryDate { get; set; }
        public string CurrentTime { get; set; }
        public decimal TotalSurcharge { get; set; }
        //public string FoodType { get; set; }
        public string ContinuePageUrl { get; set; }
        public int Infant { get; set; }
        public int Guides { get; set; }
        public string Url { get; set; }

        public int OwnTourTimeSlotId { get; set; }
        public int? PackageId { get; set; }

        public int SupplierTourOptionId { get; set; }
        public int? SupplierID { get; set; }
        public decimal WithoutOfferPrice { get; set; }

        //For Hotelbeds Tour
        public string RateKey { get; set; }
        public string XMLTourCode { get; set; }
        public string XMLTourOptionCode { get; set; }
        public string TourChildAge { get; set; }
        public bool IsXmlSupplier { get; set; }
        public string Address { get; set; }
        public bool IsGroupSearch { get; set; }
        public int IsFromApi { get; set; }
        //For GTATour
        public string DepartureCode { get; set; }
        public DateTime DueDate { get; set; }
        // public string MealTypeDate { get; set; }
        #region "Meal"
        //--------------------For New Meal--------------------------//
        public string CityList { get; set; }

        #endregion
        public Int32 GIITACode { get; set; }
        public int CityId { get; set; }
        public string LocationName { get; set; }
        public string SupplierCurrency { get; set; }
        public int StarRating { get; set; }
        public string GiataId { get; set; }
        public bool Isupsell { get; set; }
        public string OperationsDetails { get; set; }

        public int IsAmend { get; set; }
        public int AmendGuestUserId { get; set; }
        public int TransferId { get; set; }
        public bool IsCombineRoom { get; set; }

        public int CountryId { get; set; }
        public string TourStatus { get; set; }
        public string HotelContactNo { get; set; }
        public string ReservationEmail { get; set; }
        public int IsVouchered { get; set; }

        public string UserType { get; set; }
        public int isfromsupplier { get; set; }
        public int OldBookingId { get; set; }//#Pruvo added on 6-MAY-2024

        public string TechName { get; set; } //Added on 03-FEB-26
    }

    public class ClsRoomRateDetails
    {
        public string SupplierName { get; set; }
        public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string RateType { get; set; }
        public string RoomTypeId { get; set; }
        public string MealId { get; set; }
        public string Date { get; set; }
        public string DateOfWeek { get; set; }
        public string Meal { get; set; }
        public string Rate { get; set; }
        public string OrderNo { get; set; }
        public string BookingStatus { get; set; }
        public string BoardTypeID { get; set; }
        public string ContractID { get; set; }
        public string FinalRateType { get; set; }
        public decimal VatdiffAmount { get; set; }


    }

    public class _PolicyDetails
    {
        public string SupplierName { get; set; }
        public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public string MealId { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CompanyCancellationPrice { get; set; }
        public string AgentCancellationPrice { get; set; }
        public string PurchaseToken { get; set; }
        public string SPUI { get; set; }
        public string RoomSequenceNo { get; set; }
        public string CartID { get; set; }
        public string BoardTypeID { get; set; }
        public string WithInCancellationDeadLine { get; set; }
        public string GIITACode { get; set; }
        public string VatdiffAmount { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string CityId { get; set; }
        public string SupplierCurrency { get; set; }
        public string BuyingPrice { get; set; }
        public string Hit { get; set; }
    }
    public class CancelationPolicyDetailsNew
    {
        public string ContractName { get; set; }
        public string CityCode { get; set; }
        public string Board { get; set; }
        public string ShortName { get; set; }
        public string RoomTypeCode { get; set; }
        public string SHRUI { get; set; }
        public string Characteristic { get; set; }
        public string Type { get; set; }
        public string BoardType { get; set; }
        public int RoomNo { get; set; }
        public int LoyaltyPoints { get; set; }

    }

    public class ShoppingCart_DetailsNew : RoomDetailsNew
    {
        public string SupplierName { get; set; }//Supplier ID like GTA/DOTW/OTH/HotelBeds
        public string RoomTypeID { get; set; }
        public string AllocationId { get; set; }
        public decimal CompanyBuyingPrice { get; set; }
        public decimal CompanySellingPrice { get; set; }
        public decimal AgentBuyingPrice { get; set; }
        public decimal AgentSellingPrice { get; set; }
        public decimal SubAgentBuyingPice { get; set; }
        public decimal SubAgentSellingPrice { get; set; }
        public decimal FinalSellingPrice { get; set; }
        public string EssentialInformation { get; set; }
        public string OfferCode { get; set; }
        public decimal IncudedOfferDiscount { get; set; }
        public decimal GrossWitohutDiscount { get; set; }
        public string DiscountPer { get; set; }
        public string DiscountPerText { get; set; }
        public string PurchaseToken { get; set; }
        public string EachoToken { get; set; }
        public string SPUI { get; set; }
        public string MealPlanCode { get; set; }
        public string ContractID { get; set; }
        public string OfferDescription { get; set; }
        public DateTime date = DateTime.Now;
        // public DateTime MealTypeDate { get { return date; } set { date = value; } }
        public string MealTypeDate { get; set; }
        public decimal TotalSurcharge { get; set; }
        // public string MealTypeDate { get; set; }
        public string ProviderInfoName { get; set; }
        public string SupplierInfoName { get; set; }
        public string SupplierInfoVatNumber { get; set; }
        //public decimal ServiceVAT { get; set; }
        public decimal CompanyBuyingPriceExpedia { get; set; }
        public decimal GrossProfitOnline { get; set; }
        public decimal OurProfit { get; set; }
        public decimal CurrencyFactor { get; set; }
        public int SupplierId { get; set; }
        public string OTHPromoCode { get; set; }
        public string RoomMealInfo { get; set; }

        #region "meal"
        public string MealAdultRate { get; set; }
        public string MealChildRate { get; set; }

        #endregion
        public decimal AdultBuyingRate { get; set; }
        public decimal ChildBuyingRate { get; set; }
        public Int32 OldBookingDetailID { get; set; }//#Pruvo added on 6-MAY-2024 
        public string SupplierEmail { get; set; } = "";//10-APR-26

    }
    public class RoomDetailsNew
    {
        public int RoomNo { get; set; }
        public string RoomName { get; set; }
        public string SupplierRoomName { get; set; } //Added on 22-NOV-2024
        public string RoomType { get; set; }
        public int NoofAdults { get; set; }
        public int NoofChild { get; set; }
        public int Child1Age { get; set; }
        public int Child2Age { get; set; }
        public string Occupancy { get; set; }
        public decimal TotalVATPayable { get; set; }
        public decimal VATAmount { get; set; }
        public int LoyaltyPoints { get; set; }

    }

    #endregion
}
