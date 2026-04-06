using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using XConnect.Models;
using XConnect.Models.Hotel;



namespace XConnect.CommonUtilities.Interface
{
    public interface ICommonMethod
    {
        string GetCurrencyCodeByCurrencyID(int CurrencyId, DataTable dtROE);
        int GetCurrencyIdCurrencyCode(string Currency, DataTable dtROE);
        DataTable UserCartTableStructure(); //Added on 19-JAN-26
        DataTable UserCartDetailsTableStructure(); //Added on 19-JAN-26
        Task<T?> SendRequestAsync<T>(string baseUrl, string endpoint, HttpMethod method, object? parameters = null, string? token = null);
        Task<int> InsertRedisHotelSearchData(string error, string remark, int AgentId, int DB, int timeoutMs);
        Task<string> GetHotelNameForRedis(int HotelId);
        #region Redis Added on 02/12/25

        Task<DataTable> GetEnableRedisAsync();
        Task<List<string>> ActiveSupplierByAgentId(int AgentId, int Timeout = 5, int DB1CacheTime=10);
        DataTable GenerateGiataDetails();
        DataTable GenerateMappingDetails();
        #endregion
        void GetPrefferedHotelCodes(ref string Ids, ref string xmlbase, ref List<SupplierSearchConfig> SearchConfig, string SupplierName, DataTable dtPreferredDataHotel, string IsSearchFor);

        void GetXtgCurrencyByCountry(string Supplier, string CountryId, ref string currency, string Agt = "");

        double GetROEByCurrency(string Currency, DataTable dtROE);

        bool IsSuppliersWithSeperateRoomRquest(string Supplier);

        DataTable GenerateDataTableNewV2();
        DataTable GenerateRoomtypeDataTableNewV2();
        DataTable GenerateCancellationDataTableNewV2();
        DataTable GenerateoomRateBreackUpDataTableNewV2();
        DataTable GenerateHotelAllocationTableNewV2();
        DataTable GenerateComplementaryTableNewV2();
        DataTable GenerateCommonInfoTableNewV2();
        DataTable GenerateHotelMinStayNewV2();

        Task XMLRequestResponseTrack_DebugMode(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "");
        Task XMLRequestResponseTrack(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "");

        Task XMLRequestResponseTrack_Error(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "");

        Task FireAway_SQL(string Request, string Response, string Supplier, string NewSearchKey, Int32 SSTimeout, int AgentId);

        Task FireAway_SQL_ResPorcessTimeLog(string Request, string Response, string Supplier, string NewSearchKey, Int32 SSTimeout, int AgentId);

        Task FireAway_SPL_SQL(ConcurrentBag<HotelX> AllTask, int AgentId, HotelSearchParameter searchpara, bool isapi = true);

        Task FireAway_MongoDB_New(string Request, string Response, string Supplier, string NewSearchKey, Int32 SSTimeout);

        Task WriteDataToFile_MongoDB(string searchKey, AvailHotelList jsonobject);

        int GetCutOffDays(DataTable dtSuplierMarkup, string SupplierName);

        DataTable GetCountryCurrency(string CountryCode);

        DataRow setCancellationPolicyRow(ref double CompanyChargeAmount, DataRow drHotelCancellation, double CurrencyFactor, string Currency, string SupplierName, string ownsystemhotelId, string RoomNo, string RoomTypeId,
       string RoomTypeName, string MealId, DateTime FromDate, DateTime ToDate, double CompanyCancellationPrice, double AgentCancellationPrice, string BoardTypeID, double VatOnPolicy, string RoomSequenceNo = "", string WithInCancellationDeadLine = "", string PurchaseToken = "", string SPUI = "", string CartID = "", string GIITACode = "");

        Task SaveErrorLogForSPReport(string Error);

        Task<AvailHotelList> MakeCacheForHotelListMaster(string filepath, string Extension, string NewSearchKey, TokenData Tokendata, AvailHotelList finalResponse, int operation);

       // Task<AvailHotelList_Comp> ConvertToFinalResponseList_RoomMapping(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null, string AgentCode = ""); // Commnet on 30MAY2025
        Task<AvailHotelList_Comp> ConvertToFinalResponseList_RoomMapping(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null, string AgentCode = "", int Roommapping=1); //ADD On 30MAY2025

        Task<AvailHotelList_Comp> ConvertToFinalResponseList_RoomMappingv2(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null, string AgentCode = "", int Roommapping = 1);  // ADD on 29-JAN-2025
        Task<AvailHotelList_Comp> ConvertToFinalResponseList(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, bool IsFromAPI = true);
        Task<AvailHotelList_Comp> ConvertToFinalResponseList_Dag2(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null);

        Task<int> InsertHotelSearchData(SaveParameter data, int count, bool from, string SupplierTime, int agentid, string SearchKey, string IP);

        Task FireAway_APILOG(string RequestId, string Request, string Response, string Token, string ApiName, string IpAddress, int AgentId,
            string ElapsedTime, string ErrorCode, string CityId, string CustomerIpAddress, Int64 HotelId, string HotelName, string provider
            , string HotelID = "", string CheckInDate = "", string CheckOutDate = "", int NoofNights = 0, int Rooms = 0, int LogId = 0, string Search_Key = "", DateTime? EntryTime = null
            , string ReferenceNo = "", string InternalReference = "", string hostName = "", string MainURL = "", string ActiveMongoDB_ForAPI_SearchLogs = "");

        Task EnterApiLogsnew(string RequestId, string Request, string Response, string Token, string ApiName, string IpAddress, int AgentId,
            string ElapsedTime, string ErrorCode, string CityId, string CustomerIpAddress, Int64 HotelId, string HotelName, string provider
            , string HotelID = "", string CheckInDate = "", string CheckOutDate = "", int NoofNights = 0, int Rooms = 0, int LogId = 0, string Search_Key = "", DateTime? EntryTime = null
            , string ReferenceNo = "", string InternalReference = "", string hostName = "");

        DataSet GetDataSetFromXMLString(string xmlString);//190225

        string GetStarRatingString(HotelSearchParameter SearchPara);//210225

        DateTime GetTimeZoneDate(string strDate); //210225

        DataSet ReadXmlFile(string FileName); //210225

        string Star_Rating(string HotelStar);

        DataForSearch GetXmlDataForSearchNew(string Natinality, DataSet dtXMLCity, string Supplier);
        DataForSearch GetXmlDataForSearchTechwise(string Natinality, DataSet dtXMLCity, string Supplier,string TechName);

        List<StarRating> GetStarRatingStringList_xtg(HotelSearchParameter SearchPara);

        List<XMLBaseID_Search> GetXMLBAsedIdString(List<HotelCodeByCity> DtHotelDAta);
        List<HotelCodeByCity> FilterWithCityCode(List<HotelCodeByCity> DtHotelDAta, string XMLBaseID);
        List<List<XMLID_Search>> GetXMLIDString(double NoOfDiv, int Noofrecords, double count, List<HotelCodeByCity> DtHotelDAta);
        string GetHotelCodeByCOmma(List<XMLID_Search> lstxml);

        List<StarRating> GetStarRatingStringList(HotelSearchParameter SearchPara);//oth

        Task<DataTable> GetVatCountrylist(int CountryId, int CityID, string ServiceType = "");

        decimal GetcalVatAmount(decimal amount, decimal percentage);

        Task XMLRequestResponseTrack_CMLog(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "");

        List<HotelCodeByCity> FilterWithOutCity_Code(int ratingcount, int hotelcount, List<HotelCodeByCity> dtHotelCommon, List<StarRating> lstRating, string Supplier);//130325

        string GetType(int NoofAdults);

        HotelSearchParameter ResetSearchParameter(HotelSearchParameter objSearPera);

        void delayManual(int Time_delay);

        #region Recheck - 270525
        bool WriteDataToFile(string path, string json);
        string ReadCacheFile(string filepath, ref bool isError);

        double TruncateDecimalPlacesNew(double val);

        Task<string> SupplierPerformanceReportForPrebookAndBook(string SupplierName, string ReportType, int AgentId, string ResponseTimeout, string ResponseType, string Request = "", string Response = "", string Error = "");
        Task<string> AnalyticsRecheckData(string SupplierName, string ResponseType, string ResponseTimeout, string SearchKey = "", string TechName = "");
        Task<string> SupplierPerformanceReportForRecheckOnly(string SupplierName, string ReportType, int AgentId, string ResponseTimeout, string ResponseType, string Request = "", string Response = "", string Error = "");
        Task<DataTable> GetVervoRoomMappingdata(int HotelId, string HotelIds = "");

        string getMappedRoom_rlm(string originalRoomName, DataTable DtHotelInformation, int HotelId, ref string S_RoomNo);

        DataRow AddCheckPriceReq(DataRow drTemp, int srno, int usercartid, int usercartdetailid, string roomtype, string suppliername, string req);
        void ThirdPartyBookingHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website, string Searchkey = "");
        DataTable getCheckPriceReqTableStructure();

        

        void XMLRequestResponseTrack_MainDB(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "");
        Task XMLRequestResponseTrackError(string XmlRequest, string XmlResponse, string Provider, string TypeRequest);

        void ThirdPartyGetPolicyHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website);

        Task<ResponseDto> GetTrvcoResponse_report(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string SearchKey = "");

        Task<ResponseDto> Response_Supplier(string Request_Text, string APIUrl, int SearchTimeOut, string SupplieName, HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "");

        Task<ResponseDto> AbreuOnlineResponse_Supplier(string Request_Text, string APIUrl, int SearchTimeOut, string SearchKey = "", string Provider = "", string UserId = "", string Stage = "");
        string GenerateGTARequest(HotelSearchParameter objSearchPara);

        Task<ResponseDto> GetSupplierResponse(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "");
        #endregion

        Task<DataTable> MongoDB_BookingC(List<string> xmlhotelcode1);


        Task<DataTable> GetDerbysoftPolicy(string HotelId, string RoomId, string RateId);//23065

        Task<string> GetUserCartId(int agentid);

        DataTable GenerateCartTableForDatabase();

        DataTable ConvertToDataTable(List<BsonDocument> documents);

        Task<DataTable> CheckPrebookExistOrNot(DataTable dtCartTable, string SearchKey, string UniquiId);

        Task AUTO_DeactiveHotel(int Rayna_ID, int AgentId, string SupplierName, string APIName);

        Task<DataSet> getCartHotelPrice(Int64 GuestUserID, int Agentid = 0, int UserCartID = 0);

        DataTable GetRoomPriceTableStructure();
        DataTable getCardUpdateTable();

        DataTable getPriceUpdateTable();

        Task<bool> DotwUpdateStaus(string NewAllocation, int USercartID);

        Task<bool> GetRoomNameUpdate(string RoomName, int USercartID);
        Task<string> SupplierPerformanceReportForPrebookForAutocutoff(string SupplierName, string ReportType, int AgentId, string ResponseTimeout, string ResponseType, string Request = "", string Response = "", string Error = "");

        //prebook 250625
        Task<bool> GetHasKeyUpdateStaus_xtg(string HasKey, int USercartID);

        Task<bool> GetBookingCodeUpdateStatus(string BookingCode, int UsercartID);

        Task<bool> GetHasKeyUpdateStaus(string HasKey, int USercartID);

        void MergePriceCheckData(ref DataSet DsPriceUpdate, int UserCartId, DataTable dtCartUpdateTable, DataTable dtPriceUpdateTable, double serviceTot);

        Task<bool> GetEANOurProfitUpdate(double OurProfit, int USercartID);
        Task<bool> GetEANOurProfitUpdate_mk(double OurProfit, int UserCartDetailId);
        Task<bool> GetEANBookLinkUpdate(string BookLink, int USercartID);
        Task<bool> UpdateDerbySoftPrice(string HasKey, int USercartID);

        Task<bool> UpdateBookingv3Token(string UserCartDetailId, string UserCartId,string SupplierName,string RoomTypeId,string token);


        Task<List<RoomBooking>> MongoDB_BookingCList(List<string> xmlhotelcode1);
        Task<Dictionary<long, RoomBooking>> MongoDB_BookingCDictionary(List<string> xmlhotelcode1);

        Task<bool> UpdatePricechangeNuitee(string HasKey, int USercartID);

        //book 010825
        Task<int> GetReferenceByOtherIds_XConnect_Op(string refno, Int64 Bookingid, string ApiInternalRef, string SearchBy, int AgentId);

        Task<DataSet> GetUserCartDetailsBook(int GuestUserID, int AgentId, int UserCartId,string IP);

        Task<bool> GetTypeUpdateStaus_PriceTravel(int USercartID, string Type);
        Task<bool> GetTypeUpdateRateKey_PriceTravel(int USercartID, string Type);

        #region Book
        Task<DataTable> GetEmailIds(int agentID);

        Task<string> SaveShoppingCart_Pruvo(string APIInternalReference, SaveBookingDatabase item, DataTable dtPassengers, DataTable dtTourCancellation, TokenData TokenData, DataTable dtAgentVoucher, DataTable dtHotelAllocation, string CurrentDate, DataTable DtHotelRateBreakeUp, DataTable dtCheckPriceReqStructure, string OldBookRef, int OldBookingId);
        Task<string> SaveShoppingCart(string APIInternalReference, SaveBookingDatabase item, DataTable dtPassengers, DataTable dtTourCancellation, TokenData TokenData, DataTable dtAgentVoucher, DataTable dtHotelAllocation, string CurrentDate, DataTable DtHotelRateBreakeUp, DataTable dtCheckPriceReqStructure);

        Task<int> GetSupplierforgimmonix(string SupplierName);

        Task<DataTable> GetRaynaAllocationData(int RaynaId, string checkindate, string checkoutdate, string RoomNo);

        Task<string> UpdateShoppingCart_Pruvo(DataTable AgentVoucher, DataTable dtCheckPriceReq, string refeno, DataTable dtallocation, DataTable dtRedeemPoint, string IsAmend, int BookingId = 0);

        Task<string> UpdateShoppingCart(DataTable AgentVoucher, DataTable dtCheckPriceReq, string refeno, DataTable dtallocation, DataTable dtRedeemPoint, string IsAmend, int BookingId = 0);

        Task updateHotelAllocation_OTHReplica(string referenceNo);

        Task<string> AutoSettleCreditCardBooking(string rerefenceNo, string HostId = "");

        Task SaveBillingPostingDetails(string refNo, int agentId, string bookingType, int bookingId, int detailId);

        Task<DataTable> GetHotelAllocationData_SiteMinder(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId);

        Task<DataTable> ChangeBookingLimit_Siteminder(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId);

        Int64 GetRandomNo();

        Task<DataTable> ChangeBookingLimit_TravelClick(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId);
        Task<DataTable> ChangeBookingLimit_Staah(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId);
        #endregion

        #region Booking Email
        Task GetBookingMailDetail(string ReferenceNo, string ToEmail, string FromEmail, string EmpToEmailId, string EmpFromEmailId, int isConformationMail, int BookingId, int AgentId, int CompanyId,string HostName="");

        Task SendNotConfirmBooking_MultiPleRoom(string ReferenceNo, string HostName = "");

        Task UpdateTBOVoucherStatus(string referenceNumber,int UserCartDetailId,int TBOVoucherStatus);

        DataTable GetApisoBookingData(int usercartdetailsid);

        

        DataSet ReadXmlFileToDataSet(string Path = "");
        #endregion

        #region VCN
        Task<string> getWebResponse(string payload, string apiname);

        void updateCardOldData_Fail_ReBooking(string referenceNo, int bookingId, int detailId);
        #endregion


        Task<DataSet> GetShoppingCartTravelCart(string refno, Int64 Bookingid, string ApiInternalRef, string SearchBy, int AgentId);

        DataSet ReadDataFromJson(string jsonString);

        Task<DataTable> GetCheckHotelCancellationCharges(int AgentId, Int64 bookingId, string SearchBy, string ReferenceNo, string APIInternalReference);

        Task<DataTable> GetEmailIdsBranchWise(int agentID, int CountryId);

        Task SendEmailToSupplier(string rerefenceNo = "", int TempleteId = 0, DataSet dsv = null, int AgentId = 0, string HotelDetailId = "", int CancelOldBooking = 0, string HostName = "");

        Task<DataSet> GetVoucherListNew(string ReferenceNo, int BookingId, int agentId, int AmendId = 0, int CancelOldBooking = 0);

        Task<bool> CancelHotelBooking_PruvoBooking(int bookingId, decimal cancellationAmount, string cancelReason, decimal AgentCancellationAmount, string XMLRequest, string XMLResponse, bool IsAmend = false, string IP = "");

        Task<bool> CancelHotelBooking(int bookingId, decimal cancellationAmount, string cancelReason, decimal AgentCancellationAmount, string XMLRequest, string XMLResponse, bool IsAmend = false, string IP = "");

        void ThirdPartyCancellationHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website);

        Task<DataSet> GetCancellationData(int BookingId);

        Task SaveMailSendingDetails(List<Email_Details> email, int emailstatus = 0);

        Task GetBookingCancellationMail_new(string ReferenceNo, int BookingId, int countryid, string Subject, string AgentEmailId, string SubagentEmailId, string SubUserEmailId, int AgentId, int CancelOldBooking, string hostname, string branchemail, string CCEmail, string toEmail);

        void ThirdPartyErrorInCancellationHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website);

        Task<DataTable> GetPassengerDetail(int bookingDetailId);
        Task<DataTable> GetPassengerDetail_HotelRunner(int bookingDetailId);
        Task<DataTable> GetPassengerDetail_RateTiger(int bookingDetailId);
        Task<DataTable> GetPassengerDetail_Travelclick(int bookingDetailId);
        Task<DataTable> GetPassengerDetail_Rategain(int bookingDetailId);
        Task<DataTable> GetPassengerDetail_Dedge(int DetailId);
        Task<DataTable> GetPassengerDetail_Staah(int bookingDetailId);
        Task<DataSet> GetRatebreakup_Dedge(string AgentVoucherNo);
        Task<DataSet> GetRatebreakup_Stahh(string AgentVoucherNo);

        Task<bool> GetbookingkeyUpdateStaus(string pricecheck_BookingKey, int USercartID);

        // Task<bool> CheckVervopatchBookingTime(string AgentCode);//added300326
        // Task<DataTable> GetVervoTechCredentailAtRoomRequest();//added300326
        bool CheckVervopatchBookingTime(string AgentCode); //added300326
        Task<DataTable> GetVervoTechCredentailAtRoomRequest(int CacheMinutes = 5);//added300326
        bool ExcludeSupplierFromVervoMappingCheck(string SupplierName);//added300326
        Task<string> GetHotelSupplierCodeVervo(string supplierName, int Timeout = 5, int CacheMinutes = 60);
       string GetProvidersdataversion3(string Url, string UserName, string Password, List<int> HotelId, string SupplierName, int GiataId, string Searchkey, int AgentId, string MethodName, ref string VervoRequest);
    }

}
