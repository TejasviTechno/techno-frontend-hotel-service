using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics;
using System.Net;
using XConnect.Models;
using XConnect.Models.Hotel;


namespace XConnect.BAL.Interface
{
    public interface IHotelXML
    {
        Task<List<Countries>> GetCountries();

        Task<List<CitiesData>> GetCityRecord(int countryid);
        Task<bool> HotelIdRestriction(Int32 AgentID, Int32 HotelId);

        _Error GetErrorMsg(string type, string code, string description);

        Task<AvailHotelList_Comp> HotelSearch(HotelSearchParameter SearchPara, TokenData Tokendata, AdvancedOptions advancedOptions, Stopwatch MainTimer, string IP, bool withcancellation = false, string UserPlatform = "", int IsAvailbilityWithCancellationv2 = 0, string Search_Key = "");

        Task<IActionResult> CreateResponseCompressedAsync<T>(HttpContext context, HttpStatusCode statusCode, T value, string mediaType, CommonRequest RQ, object RS, TokenData tokendata, string APIName, bool EnterLogs, string ElapsedTime, string ErrorCode, string CityId, string IP = "", string HotelID = "", string CheckInDate = "", string CheckOutDate = "", int NoofNights = 0, int Rooms = 0, int LogId = 0, string Search_Key = "", DateTime? EntryTime = null, string ReferenceNo = "", string InternalReference = "", string MainURL = "", int StatusCode = 200, HotelSearchParameter payload = null, long elapsedMilliseconds = 0);

        //Task<CancellationPolicyRS_Multi_v1> ReCheck(string jsonString, TokenData Tokendata, AdvancedOptions advancedOptions, Stopwatch MainTimer,string fullUrl);



        //110425
        Task<ApiRequest<T>> SendRequestAsyncNew<T>(string apiUrl, string requestPayload, string apiName = "");

        HttpStatusCode GetStatusCode(int statusCode);

        Task<DataTable> GethealthcheckData();

        Task<List<StaticData>> GetStaticDataByCity(int CityId);

        #region Recheck - 270525
        Task<CancellationPolicyRS_Multi_v1_newapi> PolicyWithPrice(PreBookRequest policyRQ, TokenData tokenData, AdvancedOptions advancedOptions, string userAgent, string IP);
        #endregion

        Task<CancellationPilicyRS_newapi> GetCancellationPolicy(CancellationPolicyRQ policyRQ, TokenData tokenData, AdvancedOptions advancedOptions, string userAgent, string IP);
        Task<CancellationPolicyRS_Multi_newapi> GetCancellationPolicyMulti(PreBookRequest policyRQ, TokenData tokenData, AdvancedOptions advancedOptions, string userAgent, string IP);

        Task<PreBookResponse_newapi> PreBook_9(PreBookRequest prebookRQ, TokenData tokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName);
        Task<PreBookResponse_newapi> PreBook(PreBookRequest prebookRQ, TokenData tokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName,string headerValue);
        Task<CheckChargesResponse> CheckHotelCancellationCharges(CheckChargesRequest checkChargesRequest, TokenData tokenData, AdvancedOptions advancedOptions);
        Task<CancelBookingResponse_newapi> CancelBooking(CancelBookingRequest RQ, TokenData tokenData, AdvancedOptions advancedOptions, string userAgent, string IP, string HostName);
        Task<BookResponse_newapi> Book(BookRequest bookRequest, TokenData tokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName);
        Task<BookingDetailResponse_newapi> BookingDetail(BookingDetailRequest bookingDetailRequest, TokenData TokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName);

        Task<BookResponse> ReBookPruvoMarketPlace(Class_Pruvo.ReBook_PruvoMarketPlace bookRequest, TokenData tokenData, AdvancedOptions advancedOptions, string IP);
        Task<bool> GetRedisCache_oth(CacheSearchpara SearchPara, string HotelID, string checkInDate, string checkOutDate, int CityID);

        //Task StopAppPool(string appPoolName);
        //Task StartAppPool(string appPoolName);
        //Task RecycleAppPool(string appPoolName);

        #region MobileTeamNerVersion
        Task<List<StaticData>> GetStaticDataByCity(int CityId, int LoactionID, string Type);
        Task<DataTable> GetCitiesWithHotel(string CityName);
        Task<DataSet> GetHotelStaticDataOptimize(HotelSync para);
        Task<DataSet> gethotelstaticdatalistsuboptimize_v1_Address(HotelSync para);
        Task<DataSet> GetHotelStaticDataOptimizeImage(HotelSync para);
        Task<DataSet> ForPOIListing(HotelSync para);
        Task<DataSet> Filtering(HotelSync para);
        Task<DataSet> ForNeighbourHoodFiltering(HotelSync para);
        Task<DataSet> FotPropertyThemesFiltering(HotelSync para);
        Task<DataSet> FotPropertyCategoryFiltering(HotelSync para);
        Task<DataSet> FotPropertyAmenitiesFiltering(HotelSync para);
        Task<DataSet> FotBedpreferenceFiltering(HotelSync para);
        Task<DataSet> FotPropertyChainFiltering(HotelSync para);
        Task<DataSet> GetPropertyCategory(HotelSync para);
        Task<DataSet> GetPropertyPaymentCollectionInfo(HotelSync para);
        Task<DataSet> GetPropertyFees(HotelSync para);
        Task<DataSet> GetPropertyPolicies(HotelSync para);
        Task<DataSet> GetPropertyAttribute(HotelSync para);
        Task<DataSet> GetPropertyAmenities(HotelSync para);
        Task<DataSet> GetPropertyOnsitePayments(HotelSync para);
        Task<DataSet> GetPropertyRoomDescription(HotelSync para);
        Task<DataSet> GetPropertyRoomAmenities(HotelSync para);
        Task<DataSet> GETPropertyRoomImages(HotelSync para);
        Task<DataSet> GETPropertyRoomBedGroups(HotelSync para);
        Task<DataSet> GETPropertyRoomBedGroupsConfiguration(HotelSync para);
        Task<DataSet> GETPropertyRoomArea(HotelSync para);
        Task<DataSet> GETPropertyRoomViews(HotelSync para);
        Task<DataSet> GETPropertyRoomOccupancyMaxAllowed(HotelSync para);
        Task<DataSet> GetPropertyRoomOccupancyAgeCategories(HotelSync para);
        Task<DataSet> GetPropertyDescriptions(HotelSync para);
        Task<DataSet> GetPropertyThemes(HotelSync para);
        Task<DataSet> GetPropertyChain(HotelSync para);
        Task<DataSet> GetPropertyBrand(HotelSync para);
        Task<DataSet> GetPropertySpokenLanguages(HotelSync para);

        #endregion MobileTeamVersion 

        Task<BookResponse_newversion> Book_newversion(BookRequest_newversion bookRequest, TokenData tokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName);
        //Task<CancelBookingResponse_newversion> Cancel_newversion_old(CancelBookingRequest_new cancelRequest, TokenData tokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName);
        Task<CancelBookingResponse_newversion1> Cancel_newversion(CancelBookingRequest_new cancelRequest, TokenData tokenData, AdvancedOptions advancedOptions, string JsonRQ, string userAgent, string IP, string HostName);

        Task<string> RemoveAgentWiseSupplier(int AgentId);

        Task<List<GetRange>> GetRangeForAPI(GetRangeForAPI getRangeForAPI, int AgentId);
        Task<List<Agentlistlogs>> GetAgentListApiOutLogs(ApiOutLogs apiOutLogs, int AgentId);

        Task<List<Supplierlogs>> GetApiOutLogs(ApiRequest apiRequest);

    }
}
