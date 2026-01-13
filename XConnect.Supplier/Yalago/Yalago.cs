using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Yalago
{
    public class Yalago
    {
        #region "Availability Request"

        public class Room
        {
            public int Adults { get; set; }
            public List<object> ChildAges { get; set; }
        }
        public class SearchRequest
        {
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            //public int EstablishmentId { get; set; }
            public List<object> EstablishmentIds { get; set; }
            public string SourceMarket { get; set; }
            public List<Room> Rooms { get; set; }
            public string Culture { get; set; }
            public bool GetPackagePrice { get; set; }
            public bool GetTaxBreakdown { get; set; }
        }
        #endregion

        #region "Availablity Response"

        public class GrossCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }
        public class NetCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }
        public class PreOpaqueNetCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }
        public class CancellationCharge
        {
            public Charge Charge { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime ExpiryDateUTC { get; set; }

        }
        public class CancellationPolicy
        {
            public List<CancellationCharge> CancellationCharges { get; set; }

        }
        public class Board
        {
            public int Type { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public GrossCost GrossCost { get; set; }
            public NetCost NetCost { get; set; }
            public bool NonRefundable { get; set; }
            public bool IsPayAtHotel { get; set; }
            public bool IsBindingPrice { get; set; }
            public bool IsOnRequest { get; set; }
            public int RequestedRoomIndex { get; set; }
            public CancellationPolicy CancellationPolicy { get; set; }
            public object BookingConditions { get; set; }
            public object SpecialOfferText { get; set; }
            public object LongSpecialOfferText { get; set; }
            public PreOpaqueNetCost PreOpaqueNetCost { get; set; }
            public object GrossDiscount { get; set; }
            public bool IsPackagePrice { get; set; }
        }
        public class Room3
        {
            public string Code { get; set; }
            public string Description { get; set; }
            public int QuantityAvailable { get; set; }
            public int SupplierId { get; set; }
            public string SupplierName { get; set; }
            public bool IsDirectContract { get; set; }
            public List<Board> Boards { get; set; }
        }
        public class EstablishmentInfo
        {
            public string EstablishmentName { get; set; }
            public int LocationId { get; set; }
            public string LocationName { get; set; }
            public int Rating { get; set; }
            public int RatingTypeId { get; set; }
            public string AccomodationType { get; set; }
            public string CountryCode { get; set; }

        }
        public class Establishment
        {
            public int EstablishmentId { get; set; }
            public List<Room3> Rooms { get; set; }
            public EstablishmentInfo EstablishmentInfo { get; set; }
        }
        public class HotellistResponse
        {
            public List<Establishment> Establishments { get; set; }
            public List<object> Messages { get; set; }
        }


        #endregion



        #region "Cancellation policy Request"

        public class Extra
        {
            //public string ExtraId { get; set; }
            //public string OptionId { get; set; }
        }

        public class Room1
        {
            public int Adults { get; set; }
            public List<object> ChildAges { get; set; }
            public string RoomCode { get; set; }
            public string BoardCode { get; set; }
            public List<Extra> Extras { get; set; }
        }

        public class RootObject
        {
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public int EstablishmentId { get; set; }
            public List<Room1> Rooms { get; set; }
            public string Culture { get; set; }
            public bool GetPackagePrice { get; set; }
            public bool GetLocalCharges { get; set; }
            //public bool IsPackage { get; set; }
            public string SourceMarket { get; set; }
            public bool GetTaxBreakdown { get; set; }
        }
        #endregion

        #region "PriceCheck Request"

        public class RooM
        {
            public int Adults { get; set; }
            public object ChildAges { get; set; }
            public string RoomCode { get; set; }
            public string BoardCode { get; set; }
            public object Extras { get; set; }
        }

        public class Rootobject
        {
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public int EstablishmentId { get; set; }
            public List<Room> Rooms { get; set; }
            public string Culture { get; set; }
        }

        #endregion

        #region "Create Booking Request"

        public class ContactDetails
        {
            //public string Address1 { get; set; }
            //public string Address2 { get; set; }
            //public string Address3 { get; set; }
            //public string Country { get; set; }
            //public string County { get; set; }
            //public string DateOfBirth { get; set; }
            //public string EmailAddress { get; set; }
            public string FirstName { get; set; }
            //public string HomeTel { get; set; }
            public string LastName { get; set; }
            //public string MobileTel { get; set; }
            //public string PostCode { get; set; }
            public string Title { get; set; }
            //public string Town { get; set; }
            //public string WorkTel { get; set; }
        }

        public class ExpectedNetCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Guest
        {
            public int Age { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Title { get; set; }
        }

        public class room
        {
            public string AffiliateRoomRef { get; set; }
            public string RoomCode { get; set; }
            public string BoardCode { get; set; }
            public ExpectedNetCost ExpectedNetCost { get; set; }
            public List<Guest> Guests { get; set; }
            //public string SpecialRequests { get; set; }
        }

        public class rootObject
        {
            public string AffiliateRef { get; set; }
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public ContactDetails ContactDetails { get; set; }
            public string Culture { get; set; }
            public int EstablishmentId { get; set; }
            //public object PaymentMethod { get; set; }
            public List<room> Rooms { get; set; }
            public bool GetPackagePrice { get; set; }
            public string SourceMarket { get; set; }
            public bool GetTaxBreakdown { get; set; }
        }

        #endregion

        #region "Get Cancellation Charges Request"

        public class ROOtObject
        {
            public string BookingRef { get; set; }
        }

        #endregion

        #region "Cancel Booking Request"

        public class Charge
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class ExpectedCharge
        {
            public Charge Charge { get; set; }
        }

        public class rootobject
        {
            public string BookingRef { get; set; }
            public ExpectedCharge ExpectedCharge { get; set; }
        }

        #endregion


        public class BookingSatus
        {
            public class Booking1
            {
                public int BookingIndex { get; set; }
                public object BookingRef { get; set; }
                public int Status { get; set; }
                public object Establishment { get; set; }
                public object Rooms { get; set; }
                public object InfoItems { get; set; }
                public string ErrorMessage { get; set; }
                public object CheckInDate { get; set; }
                public object CheckOutDate { get; set; }
                public object AffilateRef { get; set; }
            }

            public class RootObject1
            {
                public object BookingRef { get; set; }
                public int Status { get; set; }
                public object Establishment { get; set; }
                public object Rooms { get; set; }
                public object InfoItems { get; set; }
                public string ErrorMessage { get; set; }
                public object Errors { get; set; }
                public object CheckInDate { get; set; }
                public object CheckOutDate { get; set; }
                public object AffilateRef { get; set; }
                public List<Booking> Bookings { get; set; }
            }

            public class Booking
            {
                #region "Create Booking Response"

                public class Establishment
                {
                    public int EstablishmentId { get; set; }
                    public string Name { get; set; }
                    public object EstablishmentInfo { get; set; }
                }

                public class Guest
                {
                    public string Title { get; set; }
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    public int Age { get; set; }
                }

                public class GrossCost
                {
                    public double Amount { get; set; }
                    public string Currency { get; set; }
                }

                public class NetCost
                {
                    public double Amount { get; set; }
                    public string Currency { get; set; }
                }

                public class Room
                {
                    public int BookRoomId { get; set; }
                    public string AffiliateRoomRef { get; set; }
                    public string Description { get; set; }
                    public string Board { get; set; }
                    public string RoomCode { get; set; }
                    public string BoardCode { get; set; }
                    public string ProviderRef { get; set; }
                    public string ProviderName { get; set; }
                    public List<Guest> Guests { get; set; }
                    public GrossCost GrossCost { get; set; }
                    public NetCost NetCost { get; set; }
                    public string SpecialRequests { get; set; }
                    public bool NonRefundable { get; set; }
                    public List<object> Extras { get; set; }
                    public bool IsBindingPrice { get; set; }
                }

                public class InfoItem
                {
                    public string Title { get; set; }
                    public string Description { get; set; }
                }

                public class Establishment2
                {
                    public int EstablishmentId { get; set; }
                    public string Name { get; set; }
                    public object EstablishmentInfo { get; set; }
                }

                public class Guest2
                {
                    public string Title { get; set; }
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    public int Age { get; set; }
                }

                public class GrossCost2
                {
                    public double Amount { get; set; }
                    public string Currency { get; set; }
                }

                public class NetCost2
                {
                    public double Amount { get; set; }
                    public string Currency { get; set; }
                }

                public class Room2
                {
                    public int BookRoomId { get; set; }
                    public string AffiliateRoomRef { get; set; }
                    public string Description { get; set; }
                    public string Board { get; set; }
                    public string RoomCode { get; set; }
                    public string BoardCode { get; set; }
                    public string ProviderRef { get; set; }
                    public string ProviderName { get; set; }
                    public List<Guest2> Guests { get; set; }
                    public GrossCost2 GrossCost { get; set; }
                    public NetCost2 NetCost { get; set; }
                    public string SpecialRequests { get; set; }
                    public bool NonRefundable { get; set; }
                    public List<object> Extras { get; set; }
                    public bool IsBindingPrice { get; set; }
                }

                public class InfoItem2
                {
                    public string Title { get; set; }
                    public string Description { get; set; }
                }

                public class booking
                {
                    public int BookingIndex { get; set; }
                    public string BookingRef { get; set; }
                    public int Status { get; set; }
                    public Establishment2 Establishment { get; set; }
                    public List<Room2> Rooms { get; set; }
                    public List<InfoItem2> InfoItems { get; set; }
                    public object ErrorMessage { get; set; }
                    public DateTime CheckInDate { get; set; }
                    public DateTime CheckOutDate { get; set; }
                    public string AffilateRef { get; set; }
                }

                public class RootObject
                {
                    public string BookingRef { get; set; }
                    public int Status { get; set; }
                    public Establishment Establishment { get; set; }
                    public List<Room> Rooms { get; set; }
                    public List<InfoItem> InfoItems { get; set; }
                    public object ErrorMessage { get; set; }
                    public object Errors { get; set; }
                    public DateTime CheckInDate { get; set; }
                    public DateTime CheckOutDate { get; set; }
                    public string AffilateRef { get; set; }
                    public List<Booking> Bookings { get; set; }
                }

                #endregion
            }
        }
    }

    public class Policy
    {
        public class GrossCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class NetCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Charge
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class CancellationCharge
        {
            public Charge Charge { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime ExpiryDateUTC { get; set; }
        }

        public class CancellationPolicy
        {
            public List<CancellationCharge> CancellationCharges { get; set; }
        }

        public class PreOpaqueNetCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Board
        {
            public int Type { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public GrossCost GrossCost { get; set; }
            public NetCost NetCost { get; set; }
            public bool NonRefundable { get; set; }
            public bool IsPayAtHotel { get; set; }
            public bool IsBindingPrice { get; set; }
            public bool IsOnRequest { get; set; }
            public int RequestedRoomIndex { get; set; }
            public CancellationPolicy CancellationPolicy { get; set; }
            public string BookingConditions { get; set; }
            public object SpecialOfferText { get; set; }
            public object LongSpecialOfferText { get; set; }
            public PreOpaqueNetCost PreOpaqueNetCost { get; set; }
            public object GrossDiscount { get; set; }
            public bool IsPackagePrice { get; set; }
            public List<LocalCharge> LocalCharges { get; set; }
        }

        public class Room
        {
            public string Code { get; set; }
            public string Description { get; set; }
            public int QuantityAvailable { get; set; }
            public int SupplierId { get; set; }
            public string SupplierName { get; set; }
            public bool IsDirectContract { get; set; }
            public List<Board> Boards { get; set; }
        }

        public class Establishment
        {
            public int EstablishmentId { get; set; }
            public List<Room> Rooms { get; set; }
        }
        public class LocalCharge
        {
            public object Amount { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public bool IsPerBooking { get; set; }
        }
        public class InfoItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }

        public class CancellationResponse
        {
            public Establishment Establishment { get; set; }
            public List<InfoItem> InfoItems { get; set; }
        }
    }

    public class localChargeAmount
    {
        public double Amount { get; set; }
        public string Currency { get; set; }
    }

    public class PriceCheck2
    {

        #region "PriceCheck Response"

        public class Grosscost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Netcost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Charge
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class CancellationCharge
        {
            public Charge Charge { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime ExpiryDateUTC { get; set; }
        }

        public class CancellationPolicy
        {
            public List<CancellationCharge> CancellationCharges { get; set; }
        }

        public class PreopaqueNetCost
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Discount
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class GrossDiscount
        {
            public Discount Discount { get; set; }
            public string Message { get; set; }
            public int Type { get; set; }
        }

        public class BOard
        {
            public int Type { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public Grosscost GrossCost { get; set; }
            public Netcost NetCost { get; set; }
            public bool NonRefundable { get; set; }
            public bool IsPayAtHotel { get; set; }
            public bool IsBindingPrice { get; set; }
            public bool IsOnRequest { get; set; }
            public int RequestedRoomIndex { get; set; }
            public CancellationPolicy CancellationPolicy { get; set; }
            public string BookingConditions { get; set; }
            public object SpecialOfferText { get; set; }
            public object LongSpecialOfferText { get; set; }
            public PreopaqueNetCost PreOpaqueNetCost { get; set; }
            public GrossDiscount GrossDiscount { get; set; }
            public bool IsPackagePrice { get; set; }
        }

        public class ROOM
        {
            public string Code { get; set; }
            public string Description { get; set; }
            public int QuantityAvailable { get; set; }
            public int SupplierId { get; set; }
            public string SupplierName { get; set; }
            public bool IsDirectContract { get; set; }
            public List<BOard> Boards { get; set; }
        }

        public class EstaBlishment
        {
            public int EstablishmentId { get; set; }
            public List<ROOM> Rooms { get; set; }
        }

        public class InfoItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }

        public class RootObjecT
        {
            public EstaBlishment Establishment { get; set; }
            public List<InfoItem> InfoItems { get; set; }
        }

        #endregion

    }

    public class GetCancellationChargesYalago
    {
        #region "Get Cancellation Charges Response"

        public class Charge2
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class Charge
        {
            public Charge2 charge { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime ExpiryDateUTC { get; set; }
        }

        public class Charge3
        {
            public double Amount { get; set; }
            public string Currency { get; set; }
        }

        public class CancellationCharge
        {
            public Charge3 Charge { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime ExpiryDateUTC { get; set; }
        }

        public class CancellationPolicyStatic
        {
            public List<CancellationCharge> CancellationCharges { get; set; }
            public string RoomName { get; set; }
        }

        public class RootObject
        {
            public bool IsCancellable { get; set; }
            public Charge Charge { get; set; }
            public List<CancellationPolicyStatic> CancellationPolicyStatic { get; set; }
        }

        #endregion

    }

    public class BookingcancelRSYalago
    {
        #region "Cancel Booking"

        public class GetCancelRS
        {
            public string BookingRef { get; set; }
            public int Status { get; set; }
            public object Messages { get; set; }
        }
        #endregion
    }
}
