using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class KeySettings
    {
        public string? IsHoteIdRestriction { get; set; } = String.Empty;
        public string? IsTimeCutOffActive { get; set; } = String.Empty;
        public string? IsSaveSupplierSeachLog { get; set; } = String.Empty;
        public string? SSTimeout { get; set; } = String.Empty;
        public string? ProccessCutOffTime { get; set; } = String.Empty;
        public string? StoreSupplierResErrorLog { get; set; } = String.Empty;
        public string? StoreSupplierResErrorLog_TBO_API { get; set; } = String.Empty;
        public string? ResProccessTimerLogActive { get; set; } = String.Empty;
        public string? ResProccessTimeValueforSaveLog { get; set; } = String.Empty;
        public string? Use_ReplicationDB { get; set; } = String.Empty;
        public string? staticHoteldata { get; set; } = String.Empty;
        public string? staticHoteldata_newapi { get; set; } = String.Empty;
        public string? IsSubAgentCacheActive { get; set; } = String.Empty;
        public string? UseLocalDatabase { get; set; } = String.Empty;
        public string? IsLive { get; set; } = String.Empty;
        public string? B2CMarkup { get; set; } = String.Empty;
        public string? SaveNoCountSupplog { get; set; } = String.Empty;
        public string? SaveSupplier_Error_LogActive_95 { get; set; } = String.Empty;
        public string? IsWithinearth { get; set; } = String.Empty;
        public string? SupplierPerformanceReport { get; set; } = String.Empty;
        public string? OtherAgent_Timeout { get; set; } = String.Empty;
        public string? BackgroundLogSave { get; set; } = String.Empty;
        public string? xconnectCache { get; set; } = String.Empty;
        public string? SERX_AMTJ_Timeout { get; set; } = String.Empty;
        public string? BaseCurrency { get; set; } = String.Empty;
        public string? IsOTHActive { get; set; } = String.Empty;
        public string? staticimagepath { get; set; } = String.Empty;
        public string? CM_ExTimeoutLogActive { get; set; } = String.Empty;
        public string? IsDerbySoftActive { get; set; } = String.Empty;
        public bool IsRoomReturnActive { get; set; } = false;

        public string? SupplierProxy { get; set; } = String.Empty; //170425
        public string? CancellationPolicyUrl { get; set; } = String.Empty; //170425
        public string? ReCheckUrl { get; set; } = String.Empty; //170425
        public string? PrebookUrl { get; set; } = String.Empty; //170425
        public string? BookUrl { get; set; } = String.Empty; //170425
        public string? BookingDetailUrl { get; set; } = String.Empty; //170425
        public string? cancelUrl { get; set; } = String.Empty; //170425
        public string? RecheckEndpoint { get; set; } = String.Empty; //170425
        public string? CancellationPolicyMultiEndpoint { get; set; } = String.Empty; //170425
        public string? CancellationPolicyEndpoint { get; set; } = String.Empty; //170425
        public string? PrebookEndpoint { get; set; } = String.Empty; //170425
        public string? BookEndpoint { get; set; } = String.Empty; //170425
        public string? BookingDetailEndpoint { get; set; } = String.Empty; //170425
        public string? BookingDetailV2Endpoint { get; set; } = String.Empty; //170425
        public string? CheckHotelCancellationChargesEndpoint { get; set; } = String.Empty; //170425
        public string? CancelBookingEndpoint { get; set; } = String.Empty; //170425
        public string? BookingDetail_FullUrl { get; set; } = String.Empty; //170425
        public string? BookingDetail_FullEndpoint { get; set; } = String.Empty; //170425
        
        //Recheck - 270525
        public string? CacheTimeOut { get; set; } = String.Empty; //270525
        public string? IsLogTrack { get; set; } = String.Empty; //270525
        public string? LogTrackPath { get; set; } = String.Empty; //270525

        //prebook
        public string? AutoSupplierHotelSusspend { get; set; } = String.Empty; //24-06-25

        public string? RedisCacheInsertActive { get; set; } = String.Empty;//Added on 14-JUN-2025
        //public string? ActiveIsXConnectSupplier { get; set; } //Added on 27-AUG-2025

        public string? VCNAPI_EndPoint { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? FromEmailId { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? FromBookingEmailId { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? website_url_B2Bbh { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? noemailfound { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? WLUserHost { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? WLUserPassword { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? WLUserPort { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? WLUserEmail { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? WLUserCompanyName { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? support_mailid { get; set; } = String.Empty;//Added on 17 SEP 2025
        public string? B2CBookingEmailId { get; set; } = String.Empty;//Added on 17 SEP 2025

        public string? PRODUCT_ID { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? WITHINEARTHHOLIUSD { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? WITHINEARTHHOLISAR { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? WITHINEARTHHOLIAED { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? UsdCurrencyId { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? SarCurrecnyId { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? AEDCurrencyId { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? EURCurrencyId { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? PRODUCT_ID_SAR { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? WITHINEARTHHOLIEUR { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? APISOAPIURL { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? APP_ID { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? CHANNEL_ID { get; set; } = String.Empty;//Added on 19 SEP 2025
        public string? KEY_ID { get; set; } = String.Empty;//Added on 19 SEP 2025

        public string? Active_Gimmonix { get; set; } = String.Empty;//Added on 25 SEP 2025
        public string? AdminEmail { get; set; } = String.Empty;//Added on 25 SEP 2025
        public string? CreditCardCharges { get; set; } = String.Empty;//Added on 16 DEC 2025
        public string? siteCurrency { get; set; } = String.Empty;//Added on 26-FEB-26
        public bool IsActivejsonfile { get; set; } = false; //Added on 16-april-26
        
        public string? agoda_apikey { get; set; } = String.Empty; //Added on 18-march-26
        public string? out_agoda_token { get; set; } = String.Empty; //Added on 18-march-26  
        public string? agoda_token { get; set; } = String.Empty; //Added on 27-march-26 
        public string? agoda_nationality { get; set; } = String.Empty; //Added on 19-march-26
        public string? CheckCMActiveHotel { get; set; } = String.Empty; //Added on 20-march-26 
        public string? IsXMLOutActive { get; set; } = String.Empty; //Added on 27-march-26 
        public string? EndPointXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? XMLOutAvailabilityTimeOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? XMLOutPrebookTimeOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? XMLOutBookTimeOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? XMLOutBookingDetailTimeOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? CheckHotelCancellationChargesXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? XMLOutCancelBookingTimeOut { get; set; } = String.Empty; //Added on 27-march-26 

        public string? AvailabilitywithcancellationXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? PrebookXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? BookXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? BookingDetailXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? XMLOutCancellationChargesTimeOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? CancelBookingXMLOut { get; set; } = String.Empty; //Added on 27-march-26 
        public string? TimeOutXMLOut { get; set; } = String.Empty; //Added on 25-march-26 
        public string? AddHotelToCartXMLOut { get; set; } = String.Empty; //Added on 29-apr-26 
        public string? EndPointforprebook { get; set; } = String.Empty; //Added on 29-apr-26 
        public string? EndPointforbook { get; set; } = String.Empty; //Added on 29-apr-26 
        public string? EndPointforbookingdetail { get; set; } = String.Empty; //Added on 29-apr-26 

        public bool MultiexpediaIsActive { get; set; } = false; //Added on 20-may-26
        public string? DomesticContryCode { get; set; } = String.Empty;//Added on 20-may-26
        public string? AgodaVersion { get; set; } = String.Empty;
        public string? EndPointforCheckHotelCancellationCharges { get; set; } = String.Empty; //Added on 22-may-26 
        public string? EndPointforCancelBooking { get; set; } = String.Empty; //Added on 22-may-26 
        public string? EndPointforrecheck { get; set; } = String.Empty;  
        public string? RecheckXMLOut { get; set; } = String.Empty;  
        

    }
}
