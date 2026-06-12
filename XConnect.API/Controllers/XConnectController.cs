using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;


using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using UserContextMiddleWare.Services;
using Xconnect.API.Extensions;
using Xconnect.API.Middleware;
using Xconnect.BLL;
using XConnect.Analytics;
using XConnect.Analytics.Interface;
using XConnect.Auth.Interface;
using XConnect.BAL.Interface;
using XConnect.CommonUtilities;
using XConnect.DAL;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.GRNC;
using static Azure.Core.HttpHeader;
using static Xconnect.BLL.HotelXML;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.AgodaNew.AgodaNewRepository;




namespace XConnect.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class XConnectController : ControllerBase
    {
        private readonly IAuthenticationTokenAgent _authenticationTokenAgent;
        private readonly IHotelXML _hotelXML;
        private readonly IException _exception;
        private readonly PlatformService _platformService;
        private readonly KeySettings _keySettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserContextAccessor _userContextAccessor;
        _Error error = new _Error();
        AgodaSearchError erroragoda = new AgodaSearchError();
        private readonly ILogger<Analytic> _logger;
        private readonly IAnalytics _analytic;



        public XConnectController(IAuthenticationTokenAgent authenticationTokenAgent, IHotelXML hotelXML, IException exception, PlatformService platformService, IOptions<KeySettings> keySettings, IHttpContextAccessor httpContextAccessor, IUserContextAccessor userContextAccessor, ILogger<Analytic> logger, IAnalytics analytic)
        {
            _authenticationTokenAgent = authenticationTokenAgent;
            _hotelXML = hotelXML;
            _exception = exception;
            _platformService = platformService;
            _keySettings = keySettings.Value;
            _httpContextAccessor = httpContextAccessor;
            _userContextAccessor = userContextAccessor;
            _analytic = analytic;
        }

        [Route("Countries")]
        [HttpPost]
        public async Task<IActionResult> Countries(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                //string IP = GetClientIpAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {

                    List<Countries> lstFiltOption = await _hotelXML.GetCountries();
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        //return Request.CreateResponseCompressed(HttpStatusCode.OK, (lstFiltOption), "application/json", data, "", TokenData, "Countries", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        //error = HotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    timer.Stop();
                    //return Request.CreateResponseCompressed(HttpStatusCode.Unauthorized, 401, "application/json", data, "", TokenData, "Countries", EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0");
                    return NotFound(StatusCodes.Status401Unauthorized);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }
        [Route("Cities")]
        [HttpPost]
        public async Task<IActionResult> Cities(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                //string IP = GetClientIpAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    Country payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Country>(data.Request.ToString());
                    List<CitiesData> lstFiltOption = await _hotelXML.GetCityRecord(Convert.ToInt32(payload.CountryId));
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        //return Request.CreateResponseCompressed(HttpStatusCode.OK, (lstFiltOption), "application/json", data, "", TokenData, "Countries", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        //error = HotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    timer.Stop();
                    //return Request.CreateResponseCompressed(HttpStatusCode.Unauthorized, 401, "application/json", data, "", TokenData, "Countries", EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0");
                    return NotFound(StatusCodes.Status401Unauthorized);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }


        [Route("allcitieswithcountry")]
        [HttpPost]
        public async Task<IActionResult> AllCitieswithCountry(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 1;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {


                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    GeoStaticDataRQ payload = Newtonsoft.Json.JsonConvert.DeserializeObject<GeoStaticDataRQ>(data.Request.ToString());

                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;

                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //var fullUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

                    string apiUrl = "https://testconnect.withinearth.com/api/xconnect/AllCitieswithCountry/";
                    //CancellationPolicyRS_Multi_v1 lstFiltOption = await _hotelXML.SendRequestAsync<CancellationPolicyRS_Multi_v1>(apiUrl, jsonString); //110425


                    ApiRequest<List<GeoStaticDataRS>> lstFiltOption = await _hotelXML.SendRequestAsyncNew<List<GeoStaticDataRS>>(apiUrl, jsonString);
                    int statusCode = lstFiltOption.StatusCode;

                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption.Response, "application/json", data, lstFiltOption.Response, TokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(lstFiltOption.StatusCode), lstFiltOption.Response, "application/json", data, lstFiltOption.Response, authData.tokenData, "AllCitieswithCountry", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", statusCode);


                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "AllCitieswithCountry", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                CancellationPolicyRS_Multi_v1 RS = new CancellationPolicyRS_Multi_v1();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "AllCitieswithCountry", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);

            }
        }

        [Route("availability")]
        [HttpPost]
        public async Task<IActionResult> Availability(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 1;
            DateTime EntryTime = DateTime.Now;

            //string IP = _platformService.GetIPAddress();
            string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            //string IP = GetClientIpAddress();
            string Search_Key = "";//Added on 1-MAR-2025
            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string tick = DateTime.Now.Ticks.ToString().ReverseString();
                    Search_Key = authData.tokenData.AgentCode + "_" + Guid.NewGuid().ToString().Substring(0, 5) + "_" + tick;//Added on 1-MAR-2025

                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data.Request);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 👈 Enables case-insensitive mapping
                    };


                    HotelSearchParameter payload = JsonSerializer.Deserialize<HotelSearchParameter>(jsonString, options);

                    #region Past Date Search
                    //#Due Ticket 33069
                    payload.PastDate = false;
                    if (DateTime.Now.Date > Convert.ToDateTime(payload.CheckInDate).Date)
                    {
                        payload.PastDate = true;
                    }
                    #endregion

                    if (authData.tokenData.CitycodeOverpass == 1 && payload.Filters.HotelIDs != "")//@HID
                    {
                        payload.City_ID = payload.CityID;
                        payload.CityID = 0;
                    }

                    if (payload.CountryID == 0 && Convert.ToString(payload.CountryID) != "")
                    {
                        payload.CountryID = Convert.ToInt32(payload.CountryID);
                    }
                    authData.tokenData.SearchType = "City";
                    if (authData.tokenData.CitycodeOverpass == 1 && payload.Filters.HotelIDs != "")//@HID
                    {
                        authData.tokenData.SearchType = "OverPassCity";
                        payload.City_ID = payload.CityID;
                        payload.CityID = 0;
                        payload.CountryID = 0;
                    }
                    if (payload.CountryID != 0 && payload.Filters.HotelIDs != "")
                    {
                        authData.tokenData.SearchType = "CountryID";
                        authData.tokenData.CitycodeOverpass = 1;
                    }

                    if (payload.Filters.HotelIDs == "" && payload.CountryID != 0)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Please pass Single or Multiple HotelId.");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);
                    }


                    if (_keySettings.IsHoteIdRestriction == "1" && (authData.tokenData.AgentId == 4739))
                    {
                        if (!payload.Filters.HotelIDs.Contains(","))
                        {
                            bool IsHotelIdRestriction = await _hotelXML.HotelIdRestriction(authData.tokenData.AgentId, Convert.ToInt32(payload.Filters.HotelIDs));
                            if (IsHotelIdRestriction)
                            {
                                error = _hotelXML.GetErrorMsg(ErrorType.RequestError, ErrorCode.RequestError, ErrorDescription.HotelIdRestriction);
                                timer.Stop();
                                AvailHotelList_Comp RS = new AvailHotelList_Comp();
                                RS.Error.Add(error);
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                            }
                        }
                    }

                    if (authData.tokenData.IsSearchAllow == 1)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Max Search Limit is over please contact support team");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }
                    if (Convert.ToInt32(payload.Filters.HotelIDs.Split(',').Length) > 200)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Max HotelIds limit is 200");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);


                    }
                    if (payload.Filters.HotelIDs == "" && payload.Filters.IsShowRooms == 1)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Please pass Single or Multiple HotelId when IsShowRoom:1");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }

                    for (int i = 0; i < payload.Rooms.Count; i++)
                    {
                        if (payload.Rooms[i].ChildAge.Length > 0)
                        {
                            payload.Rooms[i].Child1Age = payload.Rooms[i].ChildAge[0];
                            if (payload.Rooms[i].ChildAge.Length > 1)
                                payload.Rooms[i].Child2Age = payload.Rooms[i].ChildAge[1];
                        }
                    }
                    // int[] rating = payload.Filters.HotelIDs != "" ? 0 : payload.Filters.StarRating;
                    payload.StarStart = payload.Filters.HotelIDs != "" ? 1 : payload.Filters.StarRating.Min;
                    payload.StarEnd = payload.Filters.HotelIDs != "" ? 5 : payload.Filters.StarRating.Max;
                    payload.HotelID = payload.Filters.HotelIDs;
                    payload.Filters.IsOnlyAvailable = 1;
                    if (data.AdvancedOptions != null)
                        payload.IsWebsite = data.AdvancedOptions.Hit;

                    if (payload.Filters.StarRating != null)
                    {
                        payload.Filters.StarRating.Min = payload.Filters.StarRating.Min == 0 ? 1 : payload.Filters.StarRating.Min;
                    }

                    string validationAgentWise = payload.AgentWiseValidation(authData.tokenData.AgentId);
                    string validation = payload.ValidateAvailabilityRQ();

                    if (validation != "" || validationAgentWise != "")
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.RequestError, validation == "" ? validationAgentWise : validation);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;
                    int LogId = 0;
                    //string Search_Key = "";
                    string hotelname = "";
                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //Check Time If High then return NoCount
                    if ((_keySettings.IsTimeCutOffActive) == "1" && timer.ElapsedMilliseconds > 2000)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }


                    AvailHotelList_Comp lstFiltOption = await _hotelXML.HotelSearch(payload, authData.tokenData, data.AdvancedOptions, timer, IP, false, userAgent, 0, Search_Key);

                    data.AdvancedOptions.hotelid = hotelid;
                    data.AdvancedOptions.HotelName = hotelname;
                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.AvailabilityRS == null || lstFiltOption.AvailabilityRS.Count == 0)
                        {
                            error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                            timer.Stop();
                            AvailHotelList_Comp RS = new AvailHotelList_Comp();
                            RS.Error.Add(error);
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, Search_Key, EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                        }
                        else
                        {
                            lstFiltOption.AvailabilityRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, Search_Key, EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);
                            //return Ok(lstFiltOption);
                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.InternalError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 500, payload, timer.ElapsedMilliseconds);

                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                AvailHotelList_Comp RS = new AvailHotelList_Comp();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "availability", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);

            }
        }

        [Route("availabilitywithCancellation")]
        [HttpPost]
        public async Task<IActionResult> AvailabilitywithCancellation(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            DateTime EntryTime = DateTime.Now;

            string XSiteType = Request.Headers["X-Site-Type"].ToString();
            bool IsFromB2B = XSiteType != null && XSiteType.Trim().Equals("B2B", StringComparison.OrdinalIgnoreCase);

            string IP = _platformService.GetIPAddress();
            //string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            // string IP = GetClientIpAddress();
            string Search_Key = "";//Added on 1-MAR-2025
            try
            {
                data.IsSearch = 1;
                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent, IsRefereshCache);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    authData.tokenData.IsPackage = false;
                    authData.tokenData.IsFromB2B = IsFromB2B;

                    string tick = DateTime.Now.Ticks.ToString().ReverseString();
                    Search_Key = authData.tokenData.AgentCode + "_" + Guid.NewGuid().ToString().Substring(0, 5) + "_" + tick;//Added on 1-MAR-2025

                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data.Request);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 👈 Enables case-insensitive mapping
                    };


                    HotelSearchParameter payload = JsonSerializer.Deserialize<HotelSearchParameter>(jsonString, options);

                    #region Past Date Search
                    //#Due Ticket 33069
                    payload.PastDate = false;
                    if (DateTime.Now.Date > Convert.ToDateTime(payload.CheckInDate).Date)
                    {
                        payload.PastDate = true;
                    }
                    #endregion



                    if (authData.tokenData.CitycodeOverpass == 1 && payload.Filters.HotelIDs != "")//@HID
                    {
                        payload.City_ID = payload.CityID;
                        payload.CityID = 0;
                    }

                    if (payload.CountryID == 0 && Convert.ToString(payload.CountryID) != "")
                    {
                        payload.CountryID = Convert.ToInt32(payload.CountryID);
                    }
                    authData.tokenData.SearchType = "City";
                    if (authData.tokenData.CitycodeOverpass == 1 && payload.Filters.HotelIDs != "")//@HID
                    {
                        authData.tokenData.SearchType = "OverPassCity";
                        payload.City_ID = payload.CityID;
                        payload.CityID = 0;
                        payload.CountryID = 0;
                    }
                    if (payload.CountryID != 0 && payload.Filters.HotelIDs != "")
                    {
                        authData.tokenData.SearchType = "CountryID";
                        authData.tokenData.CitycodeOverpass = 1;
                    }

                    if (payload.Filters.HotelIDs == "" && payload.CountryID != 0)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Please pass Single or Multiple HotelId.");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);
                    }

                    if (_keySettings.IsHoteIdRestriction == "1" && (authData.tokenData.AgentId == 4739))
                    {
                        if (!payload.Filters.HotelIDs.Contains(","))
                        {
                            bool IsHotelIdRestriction = await _hotelXML.HotelIdRestriction(authData.tokenData.AgentId, Convert.ToInt32(payload.Filters.HotelIDs));
                            if (IsHotelIdRestriction)
                            {
                                error = _hotelXML.GetErrorMsg(ErrorType.RequestError, ErrorCode.RequestError, ErrorDescription.HotelIdRestriction);
                                timer.Stop();
                                AvailHotelList_Comp RS = new AvailHotelList_Comp();
                                RS.Error.Add(error);
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                            }
                        }
                    }

                    //if (authData.tokenData.IsSearchAllow == 1)
                    //{
                    //    error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Max Search Limit is over please contact support team");
                    //    timer.Stop();
                    //    AvailHotelList_Comp RS = new AvailHotelList_Comp();
                    //    RS.Error.Add(error);
                    //    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200);

                    //}
                    if (Convert.ToInt32(payload.Filters.HotelIDs.Split(',').Length) > 200)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Max HotelIds limit is 200");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);


                    }
                    if (payload.Filters.HotelIDs == "" && payload.Filters.IsShowRooms == 1)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Please pass Single or Multiple HotelId when IsShowRoom:1");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }

                    for (int i = 0; i < payload.Rooms.Count; i++)
                    {
                        if (payload.Rooms[i].ChildAge.Length > 0)
                        {
                            payload.Rooms[i].Child1Age = payload.Rooms[i].ChildAge[0];
                            if (payload.Rooms[i].ChildAge.Length > 1)
                                payload.Rooms[i].Child2Age = payload.Rooms[i].ChildAge[1];
                        }
                    }
                    // int[] rating = payload.Filters.HotelIDs != "" ? 0 : payload.Filters.StarRating;
                    payload.StarStart = payload.Filters.HotelIDs != "" ? 1 : payload.Filters.StarRating.Min;
                    payload.StarEnd = payload.Filters.HotelIDs != "" ? 5 : payload.Filters.StarRating.Max;
                    payload.HotelID = payload.Filters.HotelIDs;
                    payload.Filters.IsOnlyAvailable = 1;
                    if (data.AdvancedOptions != null)
                        payload.IsWebsite = data.AdvancedOptions.Hit;

                    if (payload.Filters.StarRating != null)
                    {
                        payload.Filters.StarRating.Min = payload.Filters.StarRating.Min == 0 ? 1 : payload.Filters.StarRating.Min;
                    }

                    string validationAgentWise = payload.AgentWiseValidation(authData.tokenData.AgentId);
                    string validation = payload.ValidateAvailabilityRQ();

                    if (validation != "" || validationAgentWise != "")
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.RequestError, validation == "" ? validationAgentWise : validation);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;
                    int LogId = 0;

                    string hotelname = "";
                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //Check Time If High then return NoCount
                    if ((_keySettings.IsTimeCutOffActive) == "1" && timer.ElapsedMilliseconds > 2000)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }

                    bool withcancellation = true;
                    payload.IsAvailbilityWithCancellation = 1;



                    AvailHotelList_Comp lstFiltOption = await _hotelXML.HotelSearch(payload, authData.tokenData, data.AdvancedOptions, timer, IP, withcancellation, userAgent, 0, Search_Key);

                    data.AdvancedOptions.hotelid = hotelid;
                    data.AdvancedOptions.HotelName = hotelname;
                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.AvailabilityRS == null || lstFiltOption.AvailabilityRS.Count == 0)
                        {
                            error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                            timer.Stop();
                            AvailHotelList_Comp RS = new AvailHotelList_Comp();
                            RS.Error.Add(error);
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, Search_Key, EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                        }
                        else
                        {
                            if (authData.tokenData.IsFromAPI)
                                lstFiltOption.AvailabilityRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            else
                                lstFiltOption.AvailabilityRS.Currency = authData.tokenData.SiteCurrency; //bcoz return in site base currency

                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, Search_Key, EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);
                            //return Ok(lstFiltOption);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.InternalError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 500, payload, timer.ElapsedMilliseconds);

                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                //_logger.LogError(ex, "AvailabilitywithCancellation");
                AvailHotelList_Comp RS = new AvailHotelList_Comp();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);

            }
        }

        [Route("availabilitywithCancellationv2")]
        [HttpPost]
        public async Task<IActionResult> AvailabilitywithCancellationv2(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 1;
            DateTime EntryTime = DateTime.Now;

            //string IP = _platformService.GetIPAddress();
            string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            // string IP = GetClientIpAddress();
            string Search_Key = "";//Added on 1-MAR-2025
            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string tick = DateTime.Now.Ticks.ToString().ReverseString();
                    Search_Key = authData.tokenData.AgentCode + "_" + Guid.NewGuid().ToString().Substring(0, 5) + "_" + tick;//Added on 1-MAR-2025

                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data.Request);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 👈 Enables case-insensitive mapping
                    };


                    HotelSearchParameter payload = JsonSerializer.Deserialize<HotelSearchParameter>(jsonString, options);

                    #region Past Date Search
                    //#Due Ticket 33069
                    payload.PastDate = false;
                    if (DateTime.Now.Date > Convert.ToDateTime(payload.CheckInDate).Date)
                    {
                        payload.PastDate = true;
                    }
                    #endregion



                    if (authData.tokenData.CitycodeOverpass == 1 && payload.Filters.HotelIDs != "")//@HID
                    {
                        payload.City_ID = payload.CityID;
                        payload.CityID = 0;
                    }

                    if (payload.CountryID == 0 && Convert.ToString(payload.CountryID) != "")
                    {
                        payload.CountryID = Convert.ToInt32(payload.CountryID);
                    }
                    authData.tokenData.SearchType = "City";
                    if (authData.tokenData.CitycodeOverpass == 1 && payload.Filters.HotelIDs != "")//@HID
                    {
                        authData.tokenData.SearchType = "OverPassCity";
                        payload.City_ID = payload.CityID;
                        payload.CityID = 0;
                        payload.CountryID = 0;
                    }
                    if (payload.CountryID != 0 && payload.Filters.HotelIDs != "")
                    {
                        authData.tokenData.SearchType = "CountryID";
                        authData.tokenData.CitycodeOverpass = 1;
                    }

                    if (payload.Filters.HotelIDs == "" && payload.CountryID != 0)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Please pass Single or Multiple HotelId.");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);
                    }

                    if (_keySettings.IsHoteIdRestriction == "1" && (authData.tokenData.AgentId == 4739))
                    {
                        if (!payload.Filters.HotelIDs.Contains(","))
                        {
                            bool IsHotelIdRestriction = await _hotelXML.HotelIdRestriction(authData.tokenData.AgentId, Convert.ToInt32(payload.Filters.HotelIDs));
                            if (IsHotelIdRestriction)
                            {
                                error = _hotelXML.GetErrorMsg(ErrorType.RequestError, ErrorCode.RequestError, ErrorDescription.HotelIdRestriction);
                                timer.Stop();
                                AvailHotelList_Comp RS = new AvailHotelList_Comp();
                                RS.Error.Add(error);
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                            }
                        }
                    }

                    if (authData.tokenData.IsSearchAllow == 1)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Max Search Limit is over please contact support team");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }
                    if (Convert.ToInt32(payload.Filters.HotelIDs.Split(',').Length) > 200)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Max HotelIds limit is 200");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);


                    }
                    if (payload.Filters.HotelIDs == "" && payload.Filters.IsShowRooms == 1)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.NoPermission, ErrorCode.NoPermission, "Please pass Single or Multiple HotelId when IsShowRoom:1");
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }

                    for (int i = 0; i < payload.Rooms.Count; i++)
                    {
                        if (payload.Rooms[i].ChildAge.Length > 0)
                        {
                            payload.Rooms[i].Child1Age = payload.Rooms[i].ChildAge[0];
                            if (payload.Rooms[i].ChildAge.Length > 1)
                                payload.Rooms[i].Child2Age = payload.Rooms[i].ChildAge[1];
                        }
                    }
                    // int[] rating = payload.Filters.HotelIDs != "" ? 0 : payload.Filters.StarRating;
                    payload.StarStart = payload.Filters.HotelIDs != "" ? 1 : payload.Filters.StarRating.Min;
                    payload.StarEnd = payload.Filters.HotelIDs != "" ? 5 : payload.Filters.StarRating.Max;
                    payload.HotelID = payload.Filters.HotelIDs;
                    payload.Filters.IsOnlyAvailable = 1;
                    if (data.AdvancedOptions != null)
                        payload.IsWebsite = data.AdvancedOptions.Hit;

                    if (payload.Filters.StarRating != null)
                    {
                        payload.Filters.StarRating.Min = payload.Filters.StarRating.Min == 0 ? 1 : payload.Filters.StarRating.Min;
                    }

                    string validationAgentWise = payload.AgentWiseValidation(authData.tokenData.AgentId);
                    string validation = payload.ValidateAvailabilityRQ();

                    if (validation != "" || validationAgentWise != "")
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.RequestError, validation == "" ? validationAgentWise : validation);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.BadRequest, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.RequestError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;
                    int LogId = 0;
                    //string Search_Key = "";
                    string hotelname = "";
                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //Check Time If High then return NoCount
                    if ((_keySettings.IsTimeCutOffActive) == "1" && timer.ElapsedMilliseconds > 2000)
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, "", EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                    }

                    bool withcancellation = true;
                    payload.IsAvailbilityWithCancellation = 1;
                    payload.IsAvailbilityWithCancellationv2 = 1; // Add On 29-JAN-2025

                    AvailHotelList_Comp lstFiltOption = await _hotelXML.HotelSearch(payload, authData.tokenData, data.AdvancedOptions, timer, IP, withcancellation, userAgent, payload.IsAvailbilityWithCancellationv2, Search_Key);

                    data.AdvancedOptions.hotelid = hotelid;
                    data.AdvancedOptions.HotelName = hotelname;
                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.AvailabilityRS == null || lstFiltOption.AvailabilityRS.Count == 0)
                        {
                            error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                            timer.Stop();
                            AvailHotelList_Comp RS = new AvailHotelList_Comp();
                            RS.Error.Add(error);
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, Search_Key, EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);

                        }
                        else
                        {
                            lstFiltOption.AvailabilityRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.CityID.ToString(), IP, Convert.ToString(payload.Filters.HotelIDs), Convert.ToString(payload.CheckInDate), Convert.ToString(payload.CheckOutDate), payload.NoofNights, payload.Rooms.Count, 0, Search_Key, EntryTime, "", "", "", 200, payload, timer.ElapsedMilliseconds);
                            //return Ok(lstFiltOption);
                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        AvailHotelList_Comp RS = new AvailHotelList_Comp();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.InternalError, payload.CityID.ToString(), IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 500, payload, timer.ElapsedMilliseconds);

                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                AvailHotelList_Comp RS = new AvailHotelList_Comp();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "availabilitywithCancellationv2", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Search_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);

            }
        }


        [Route("ReCheck_old")]
        [HttpPost]
        public async Task<IActionResult> ReCheck_old(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2; //230425
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {


                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());

                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string hotelid = "0";

                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //var fullUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/ReCheck_dotN9/";
                    string apiUrl = _keySettings.ReCheckUrl + _keySettings.RecheckEndpoint; //230425
                    //CancellationPolicyRS_Multi_v1 lstFiltOption = await _hotelXML.SendRequestAsync<CancellationPolicyRS_Multi_v1>(apiUrl, jsonString); //110425


                    //ApiRequest<CancellationPolicyRS_Multi_v1> lstFiltOption = await _hotelXML.SendRequestAsyncNew<CancellationPolicyRS_Multi_v1>(apiUrl, jsonString);
                    ApiRequest<CancellationPolicyRS_Multi_v1_Supp> lstFiltOption1 = await _hotelXML.SendRequestAsyncNew<CancellationPolicyRS_Multi_v1_Supp>(apiUrl, jsonString, "recheck");
                    int statusCode = lstFiltOption1.StatusCode;
                    if (statusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);
                    }

                    try
                    {
                        data.AdvancedOptions.Provider = lstFiltOption1.Response.Provider;//220425
                        hotelid = lstFiltOption1.Response.HotelId;//220425
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }


                    Seach_Key = payload.Searchkey;



                    CancellationPolicyRS_Multi_v1 lstFiltOption = lstFiltOption1.Response != null ? lstFiltOption1.Response.CancellationPolicyRS_Multi_v1 : null;


                    if (lstFiltOption != null)
                    {

                        if (lstFiltOption.Error.Count > 0) //21-04-25
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(statusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", statusCode);
                        }
                        else
                        {
                            bool IsNotAvailable = false;
                            for (int i = 0; i < lstFiltOption.ReCheckRS.Count; i++)
                            {
                                if (lstFiltOption.ReCheckRS[i].BookingStatus == "NotAvailable")
                                {
                                    IsNotAvailable = true;
                                }
                            }
                            if (IsNotAvailable)
                            {
                                lstFiltOption.Currency = authData.tokenData.CreditLimitCurrencyName;
                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(statusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", statusCode);
                            }
                            else
                            {
                                for (int x = 0; x < lstFiltOption.ReCheckRS.Count; x++)
                                {
                                    lstFiltOption.ReCheckRS[x].EssentialInformation = lstFiltOption.ReCheckRS[x].EssentialInformation + EssentialInfo.Essentialinformation;
                                }
                                lstFiltOption.Currency = authData.tokenData.CreditLimitCurrencyName;
                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(statusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", statusCode);

                            }
                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        CancellationPolicyRS_Multi_v1 RS = new CancellationPolicyRS_Multi_v1();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }



                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption.Response, "application/json", data, lstFiltOption.Response, TokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(lstFiltOption.StatusCode), lstFiltOption.Response, "application/json", data, lstFiltOption.Response, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime,"","","", statusCode);


                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                CancellationPolicyRS_Multi_v1 RS = new CancellationPolicyRS_Multi_v1();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);

            }
        }

        [Route("CancellationPolicyMulti")]
        [HttpPost]
        public async Task<IActionResult> CancellationPolicyMulti(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string hotelid = "0";
                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/CancellationPolicyMulti/";
                    string apiUrl = _keySettings.CancellationPolicyUrl + _keySettings.CancellationPolicyMultiEndpoint; //230425

                    //CancellationPolicyRS_Multi lstFiltOption = await _hotelXML.SendRequestAsync<CancellationPolicyRS_Multi>(apiUrl, jsonString);
                    //ApiRequest<CancellationPolicyRS_Multi> lstFiltOption = await _hotelXML.SendRequestAsyncNew<CancellationPolicyRS_Multi>(apiUrl, jsonString);
                    ApiRequest<CancellationPolicyRS_Multi_Supp> lstFiltOption1 = await _hotelXML.SendRequestAsyncNew<CancellationPolicyRS_Multi_Supp>(apiUrl, jsonString);
                    int StatusCode = lstFiltOption1.StatusCode;
                    Seach_Key = payload.Searchkey;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);
                    }

                    CancellationPolicyRS_Multi lstFiltOption = lstFiltOption1.Response.CancellationPolicyRS_Multi;
                    try
                    {
                        hotelid = lstFiltOption1.Response.HotelId;//220425
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }

                    if (lstFiltOption != null)
                    {

                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);

                        }
                        else
                        {
                            lstFiltOption.Currency = authData.tokenData.CreditLimitCurrencyName;
                            for (int x = 0; x < lstFiltOption.CancellationRS.Count; x++)
                            {
                                lstFiltOption.CancellationRS[x].EssentialInformation = lstFiltOption.CancellationRS[x].EssentialInformation + EssentialInfo.Essentialinformation;
                            }
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        CancellationPolicyRS_Multi_v1 RS = new CancellationPolicyRS_Multi_v1();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }

                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption.Response, "application/json", data, lstFiltOption, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", "", "", "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(lstFiltOption.StatusCode), lstFiltOption.Response, "application/json", data, lstFiltOption.Response, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime,"","","", StatusCode);

                    #region Not Needed
                    //data.AdvancedOptions.hotelid = hotelid;
                    //Seach_Key = payload.Searchkey;


                    //if (lstFiltOption != null)
                    //{
                    //    if (lstFiltOption.Error.Count > 0)
                    //    {
                    //        timer.Stop();

                    //        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //    }
                    //    else
                    //    {

                    //        lstFiltOption.Currency = TokenData.CreditLimitCurrencyName;
                    //        for (int x = 0; x < lstFiltOption.CancellationRS.Count; x++)
                    //        {
                    //            lstFiltOption.CancellationRS[x].EssentialInformation = lstFiltOption.CancellationRS[x].EssentialInformation + EssentialInfo.Essentialinformation;
                    //        }
                    //        timer.Stop();
                    //        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //    }

                    //}
                    //else
                    //{
                    //    //Common.Error(ex);
                    //    error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                    //    lstFiltOption.Error.Add(error);
                    //    timer.Stop();
                    //    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //}
                    #endregion
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                CancellationPolicyRS_Multi_v1 RS = new CancellationPolicyRS_Multi_v1();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }


        [Route("CancellationPolicy")]
        [HttpPost]
        public async Task<IActionResult> CancellationPolicy(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    CancellationPolicyRQ payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CancellationPolicyRQ>(data.Request.ToString());


                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string hotelid = "0";

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/CancellationPolicy/";
                    string apiUrl = _keySettings.CancellationPolicyUrl + _keySettings.CancellationPolicyEndpoint; //230425

                    //CancellationPilicyRS lstFiltOption = await _hotelXML.SendRequestAsync<CancellationPilicyRS>(apiUrl, jsonString);
                    //ApiRequest<CancellationPilicyRS> lstFiltOption = await _hotelXML.SendRequestAsyncNew<CancellationPilicyRS>(apiUrl, jsonString);
                    ApiRequest<CancellationPilicyRS_Supp> lstFiltOption1 = await _hotelXML.SendRequestAsyncNew<CancellationPilicyRS_Supp>(apiUrl, jsonString);
                    int StatusCode = lstFiltOption1.StatusCode;

                    Seach_Key = payload.SearchKey;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);
                    }

                    try
                    {
                        hotelid = lstFiltOption1.Response.HotelId;
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }

                    CancellationPilicyRS lstFiltOption = lstFiltOption1.Response.CancellationPilicyRS;

                    if (lstFiltOption != null)
                    {

                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);

                        }
                        else
                        {
                            lstFiltOption.CancellationRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            lstFiltOption.CancellationRS.EssentialInformation = lstFiltOption.CancellationRS.EssentialInformation + EssentialInfo.Essentialinformation;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        CancellationPilicyRS RS = new CancellationPilicyRS();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, hotelid, "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }


                    // return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption.Response, "application/json", data, lstFiltOption.Response, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime,"","","", StatusCode);

                    #region No Needed
                    //data.AdvancedOptions.hotelid = hotelid;
                    //Seach_Key = payload.SearchKey;


                    //if (lstFiltOption != null)
                    //{
                    //    if (lstFiltOption.Error.Count > 0)
                    //    {
                    //        timer.Stop();

                    //        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //    }
                    //    else
                    //    {

                    //        lstFiltOption.CancellationRS.Currency = TokenData.CreditLimitCurrencyName;
                    //        lstFiltOption.CancellationRS.EssentialInformation = lstFiltOption.CancellationRS.EssentialInformation + EssentialInfo.Essentialinformation;
                    //        timer.Stop();
                    //        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //    }

                    //}
                    //else
                    //{
                    //    //Common.Error(ex);
                    //    error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                    //    lstFiltOption.Error.Add(error);
                    //    timer.Stop();
                    //    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", "", Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime);
                    //}
                    #endregion
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                CancellationPilicyRS RS = new CancellationPilicyRS();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }

        [Route("PreBook_old")]
        [HttpPost]
        public async Task<IActionResult> PreBook_old(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {


                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());


                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;

                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    var fullUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

                    // string apiUrl = "https://testconnect.withinearth.com/api/xconnect/PreBook/";
                    string apiUrl = _keySettings.PrebookUrl + _keySettings.PrebookEndpoint; //170425

                    //PreBookResponse RS = await _hotelXML.SendRequestAsync<PreBookResponse>(apiUrl, jsonString);
                    //ApiRequest<PreBookResponse> RS = await _hotelXML.SendRequestAsyncNew<PreBookResponse>(apiUrl, jsonString);
                    ApiRequest<PreBookResponse_Supp> RS1 = await _hotelXML.SendRequestAsyncNew<PreBookResponse_Supp>(apiUrl, jsonString);

                    int StatusCode = RS1.StatusCode;
                    Seach_Key = payload.Searchkey;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);
                    }


                    try
                    {
                        data.AdvancedOptions.Provider = RS1.Response.Provider;//230425
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }



                    PreBookResponse RS = RS1.Response != null ? RS1.Response.PreBookResponse : null;

                    if (RS != null)
                    {

                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                await _exception.Error(x);
                            }
                            if (RS.PreBookRS != null)
                            {
                                RS.PreBookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), Error_Code, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);

                            }
                            else
                            {
                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), Error_Code, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);

                            }
                        }
                        else
                        {
                            RS.PreBookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", StatusCode);
                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        PreBookResponse RSS = new PreBookResponse();
                        RSS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }

                    // return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0",IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime,"", "", "", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                PreBookResponse RSS = new PreBookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);

                RSS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, TokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }

        [Route("Book")]
        [HttpPost]
        public async Task<IActionResult> Book(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {


                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);

                    BookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<BookRequest>(data.Request.ToString());

                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;

                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/Book/";
                    string apiUrl = _keySettings.BookUrl + _keySettings.BookEndpoint; //170425

                    //BookResponse RS = await _hotelXML.SendRequestAsync<BookResponse>(apiUrl, jsonString);
                    //ApiRequest<BookResponse> RS = await _hotelXML.SendRequestAsyncNew<BookResponse>(apiUrl, jsonString);
                    ApiRequest<BookResponse_Supp> RS1 = await _hotelXML.SendRequestAsyncNew<BookResponse_Supp>(apiUrl, jsonString, "book");//230425
                    int StatusCode = RS1.StatusCode;

                    string Ref = "";
                    string IntRef = "";

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, Ref, IntRef, "", 401, null, timer.ElapsedMilliseconds);
                    }

                    try
                    {
                        data.AdvancedOptions.Provider = RS1.Response.Provider;

                    }
                    catch (Exception ex)
                    {

                        await _exception.Error(ex);
                    }

                    Seach_Key = payload.SearchKey;
                    BookResponse RS = RS1.Response != null ? RS1.Response.BookResponse : null;






                    if (RS != null)
                    {
                        try
                        {
                            Ref = RS.BookRS.ReferenceNo;
                            IntRef = RS.BookRS.InternalReference;

                        }
                        catch (Exception ex)
                        {
                            await _exception.Error(ex);
                        }


                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                await _exception.Error(x);
                            }
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, _hotelXML.GetStatusCode(StatusCode), RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), Error_Code, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, Ref, IntRef, "", StatusCode, null, timer.ElapsedMilliseconds);
                        }
                        else
                        {
                            timer.Stop();
                            RS.BookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime, Ref, IntRef, "", StatusCode, null, timer.ElapsedMilliseconds);

                        }
                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + ".");
                        BookResponse RSS = new BookResponse();
                        RSS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, Ref, IntRef, "", 500, null, timer.ElapsedMilliseconds);

                    }

                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(hotelid), "", "", 0, 0, 0, Seach_Key, EntryTime,"", "", "", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.ErrorBooking(ex, "001");
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "__" + ex.Message);
                BookResponse RS = new BookResponse();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);
            }
        }

        [Route("BookingDetail")]
        [HttpPost]
        public async Task<IActionResult> BookingDetail(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string Ref = "";
                    string IntRef = "";

                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);

                    BookingDetailRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingDetailRequest>(data.Request.ToString());

                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;

                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/BookingDetail/";
                    string apiUrl = _keySettings.BookingDetailUrl + _keySettings.BookingDetailEndpoint; //170425

                    //BookingDetailResponse RS = await _hotelXML.SendRequestAsync<BookingDetailResponse>(apiUrl, jsonString);
                    //ApiRequest<BookingDetailResponse> RS = await _hotelXML.SendRequestAsyncNew<BookingDetailResponse>(apiUrl, jsonString);
                    ApiRequest<BookingDetailResponse_Supp> RS1 = await _hotelXML.SendRequestAsyncNew<BookingDetailResponse_Supp>(apiUrl, jsonString, "bookingdetail");
                    int StatusCode = RS1.StatusCode;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, "", "", "", 401);
                    }

                    try
                    {
                        data.AdvancedOptions.Provider = RS1.Response != null ? RS1.Response.Provider : "";
                        Ref = RS1.Response != null ? RS1.Response.Ref : "";
                        IntRef = RS1.Response != null ? RS1.Response.IntRef : "";
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }

                    BookingDetailResponse RS = RS1.Response != null ? RS1.Response.BookingDetailResponse : null;

                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, null, Ref, IntRef, "", StatusCode);

                        }
                        else
                        {
                            RS.BookingDetailRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, null, Ref, IntRef, "", StatusCode);

                        }
                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + ".");
                        BookingDetailResponse RSS = new BookingDetailResponse();
                        RSS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", 500);
                    }


                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, null, payload.BookingDetailRQ.ReferenceNo,"","", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "__" + ex.Message);
                BookingDetailResponse RS = new BookingDetailResponse();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }

        [Route("BookingDetailV2")]
        [HttpPost]
        public async Task<IActionResult> BookingDetailV2(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());
                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string Ref = "";
                    string IntRef = "";

                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);

                    BookingDetailRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingDetailRequest>(data.Request.ToString());

                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    Int64 hotelid = 0;

                    data.AdvancedOptions.CustomerIpAddress = data.AdvancedOptions.CustomerIpAddress == null ? IP : data.AdvancedOptions.CustomerIpAddress;

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/BookingDetailV2/";
                    string apiUrl = _keySettings.BookingDetailUrl + _keySettings.BookingDetailV2Endpoint; //170425

                    //BookingDetailResponse_dmc RS = await _hotelXML.SendRequestAsync<BookingDetailResponse_dmc>(apiUrl, jsonString);
                    //ApiRequest<BookingDetailResponse_dmc> RS = await _hotelXML.SendRequestAsyncNew<BookingDetailResponse_dmc>(apiUrl, jsonString);
                    ApiRequest<BookingDetailResponse_dmc_Supp> RS1 = await _hotelXML.SendRequestAsyncNew<BookingDetailResponse_dmc_Supp>(apiUrl, jsonString);
                    int StatusCode = RS1.StatusCode;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, "", "", "", 401);
                    }
                    try
                    {
                        data.AdvancedOptions.Provider = RS1.Response != null ? RS1.Response.Provider : "";
                        Ref = RS1.Response != null ? RS1.Response.Ref : "";
                        IntRef = RS1.Response != null ? RS1.Response.IntRef : "";
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    BookingDetailResponse_dmc RS = RS1.Response != null ? RS1.Response.BookingDetailResponse_dmc : null;


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", StatusCode);

                        }
                        else
                        {
                            RS.BookingDetailRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", StatusCode);

                        }
                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + ".");
                        BookingDetailResponse RSS = new BookingDetailResponse();
                        RSS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", 500);
                    }

                    // return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, null, payload.BookingDetailRQ.ReferenceNo, "","", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "__" + ex.Message);
                BookingDetailResponse RS = new BookingDetailResponse();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "BookingDetailV2", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }

        [Route("CheckHotelCancellationCharges")]
        [HttpPost]
        public async Task<IActionResult> CheckHotelCancellationCharges(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    CheckChargesRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckChargesRequest>(data.Request.ToString());

                    if (payload.CheckHotelCancellationChargesRQ.InternalReference == null)
                        payload.CheckHotelCancellationChargesRQ.InternalReference = "";

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/CheckHotelCancellationCharges/";
                    string apiUrl = _keySettings.cancelUrl + _keySettings.CheckHotelCancellationChargesEndpoint; //170425

                    //CheckChargesResponse RS = await _hotelXML.SendRequestAsync<CheckChargesResponse>(apiUrl, jsonString);
                    ApiRequest<CheckChargesResponse> RS = await _hotelXML.SendRequestAsyncNew<CheckChargesResponse>(apiUrl, jsonString);
                    int StatusCode = RS.StatusCode;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", 401);
                    }

                    if (RS.Response != null)
                    {
                        if (RS.Response.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, null, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", StatusCode);

                        }
                        else if (RS.Response.CheckHotelCancellationChargesRS.HotelOption.HotelRooms.Count == 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, TokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", StatusCode);

                        }
                        else
                        {
                            RS.Response.CheckHotelCancellationChargesRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", StatusCode);
                        }
                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        CheckChargesResponse RSS = new CheckChargesResponse();
                        RSS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", 500);
                    }

                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, null, payload.CheckHotelCancellationChargesRQ.ReferenceNo, "","", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                BookResponse RS = new BookResponse();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }

        [Route("CancelBooking")]
        [HttpPost]
        public async Task<IActionResult> CancelBooking(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string PNRNo = "";
                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    CancelBookingRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CancelBookingRequest>(data.Request.ToString());

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/CancelBooking/";
                    string apiUrl = _keySettings.cancelUrl + _keySettings.CancelBookingEndpoint; //170425

                    //CancelBookingResponse RS = await _hotelXML.SendRequestAsync<CancelBookingResponse>(apiUrl, jsonString);
                    //ApiRequest<CancelBookingResponse> RS = await _hotelXML.SendRequestAsyncNew<CancelBookingResponse>(apiUrl, jsonString);
                    ApiRequest<CancelBookingResponse_Supp> RS1 = await _hotelXML.SendRequestAsyncNew<CancelBookingResponse_Supp>(apiUrl, jsonString);


                    int StatusCode = RS1.StatusCode;


                    PNRNo = RS1.Response.PNRNo;

                    CancelBookingResponse RS = RS1.Response.CancelBookingResponse;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", 401, null, timer.ElapsedMilliseconds);
                    }

                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", StatusCode, null, timer.ElapsedMilliseconds);
                        }
                        else if (RS.CancelRS.HotelOption.HotelRooms.Count == 0)
                        {

                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", StatusCode, null, timer.ElapsedMilliseconds);
                        }
                        else
                        {
                            RS.CancelRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", StatusCode, null, timer.ElapsedMilliseconds);
                        }
                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        CancelBookingResponse RSS = new CancelBookingResponse();
                        RSS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, payload.CancelRQ.InternalReference, EntryTime, "", payload.CancelRQ.InternalReference, "", 500, null, timer.ElapsedMilliseconds);
                    }

                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, "", null, "", "","", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                CancelBookingResponse RS = new CancelBookingResponse();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);
            }
        }

        //[HttpGet]
        //[Route("healthcheck")]
        //public string healthcheck()
        //{
        //    return "ok";
        //}

        [Route("BookingDetail_Full")]
        [HttpPost]
        public async Task<IActionResult> BookingDetail_Full(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            data.IsSearch = 2;
            string IP = _platformService.GetIPAddress();
            // string IP = GetClientIpAddress();
            string Seach_Key = "";
            DateTime EntryTime = DateTime.Now;

            try
            {

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId) && (authData.tokenData.AgentId == 9576 || authData.tokenData.AgentId == 2172))// Allow only for 1 Agent
                {
                    DataforLog log = new DataforLog();
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only

                    string Provider = "";
                    long hotelid = 0;
                    string HotelName = "";
                    string Ref = "";
                    string IntRef = "";

                    string jsonString = data.Request is string ? (string)data.Request : JsonSerializer.Serialize(data);
                    Class_Pruvo.RQ payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Class_Pruvo.RQ>(data.Request.ToString());

                    //string apiUrl = "https://testconnect.withinearth.com/api/xconnect/CancelBooking/";
                    string apiUrl = _keySettings.BookingDetail_FullUrl + _keySettings.BookingDetail_FullEndpoint; //170425

                    //CancelBookingResponse RS = await _hotelXML.SendRequestAsync<CancelBookingResponse>(apiUrl, jsonString);
                    //ApiRequest<CancelBookingResponse> RS = await _hotelXML.SendRequestAsyncNew<CancelBookingResponse>(apiUrl, jsonString);
                    ApiRequest<Class_Pruvo.Rootobject_supp> RS1 = await _hotelXML.SendRequestAsyncNew<Class_Pruvo.Rootobject_supp>(apiUrl, jsonString);

                    int StatusCode = RS1.StatusCode;


                    Provider = RS1.Response.Provider;
                    hotelid = RS1.Response.hotelid;
                    HotelName = RS1.Response.HotelName;
                    Ref = RS1.Response.Ref;
                    IntRef = RS1.Response.IntRef;

                    Class_Pruvo.Rootobject RS = RS1.Response.Rootobject;

                    if (StatusCode == 401) //21-04-25
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetail_Full", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Ref, EntryTime, Ref, IntRef, "", 401);
                    }

                    if (RS != null)
                    {
                        if (RS.Error != null && RS.BookingDetailRS != null)
                        {
                            timer.Stop();
                            //RS.BookingDetailRS.Currency = authData.tokenData.CreditLimitCurrencyName;

                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetail_Full", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, Ref, EntryTime, Ref, IntRef, "", StatusCode);
                        }
                        else
                        {

                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetail_Full", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, Ref, EntryTime, Ref, IntRef, "", StatusCode);
                        }
                    }
                    else
                    {
                        timer.Stop();
                        Class_Pruvo._Error errorx = new Class_Pruvo._Error();
                        errorx.type = ErrorType.GeneralError;
                        errorx.code = ErrorCode.NorecordError;
                        errorx.description = ErrorDescription.NorecordError;

                        Class_Pruvo.Rootobject RSS = new Class_Pruvo.Rootobject();
                        RSS.Error.Add(errorx);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, authData.tokenData, "BookingDetail_Full", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Ref, EntryTime, Ref, IntRef, "", 500);
                    }

                    //return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS.Response, "application/json", data, RS.Response, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, "", null, "", "","", StatusCode);

                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetail_Full", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                Class_Pruvo._Error errorx = new Class_Pruvo._Error();
                errorx.type = ErrorType.GeneralError;
                errorx.code = ErrorCode.NorecordError;
                errorx.description = ErrorDescription.NorecordError;

                Class_Pruvo.Rootobject RSS = new Class_Pruvo.Rootobject();
                RSS.Error.Add(errorx);

                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RSS, "application/json", data, RSS, TokenData, "BookingDetail_Full", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }
        }

        [HttpGet]
        [Route("healthcheck")]
        public async Task<IActionResult> healthcheck() //Added on 29-APR-2025
        {
            bool isHealthy = true;// CheckAppHealth(); // Replace with your actual health check logic
            try
            {
                DataTable Tokendata = await _hotelXML.GethealthcheckData();
                string hostName = System.Net.Dns.GetHostName();

                switch (hostName)
                {
                    case "API1":
                        hostName = "API1";
                        break;
                    case "API2":
                        hostName = "API2";
                        break;
                    case "API-3":
                        hostName = "API3";
                        break;
                    case "API4":
                        hostName = "API4";
                        break;
                    case "API5":
                        hostName = "API5";
                        break;
                    case "API-6":
                        hostName = "API6";
                        break;
                    case "API-7":
                        hostName = "API7";
                        break;
                    default:
                        hostName = "other";
                        break;
                }
                isHealthy = Convert.ToBoolean(Tokendata.Rows[0][hostName]);
            }
            catch (Exception x)
            {
                isHealthy = true;
            }

            if (isHealthy)
            {
                return new ObjectResult("ok")
                {
                    StatusCode = 200
                };
            }
            else
            {
                return new ObjectResult("unhealthy")
                {
                    StatusCode = 500
                };
            }
        }

        [HttpGet]
        [Route("GetThreadInfo")]
        public string GetThreadInfo()
        {
            int workerThreads, completionPortThreads;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            int maxWorkerThreads, maxCompletionPortThreads;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            int usedWorkerThreads = maxWorkerThreads - workerThreads;
            return "AvailableThreads:" + workerThreads + ", MaxThreads:" + maxWorkerThreads + ", UsedWorkerThreads:" + usedWorkerThreads + "";
        }

        #region IpAddress
        //public string GetClientIpAddress()
        //{
        //    var httpContext = _httpContextAccessor.HttpContext;
        //    if (httpContext == null)
        //        return "Unknown";

        //    string ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        //    if (string.IsNullOrEmpty(ip))
        //        ip = httpContext.Connection.RemoteIpAddress?.ToString();

        //    return ip ?? "Unknown";
        //}
        #endregion

        #region New EndPoint

        [HttpPost]
        [Route("CancellationPolicy_9")]
        public async Task<IActionResult> CancellationPolicy_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only

                    CancellationPolicyRQ payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CancellationPolicyRQ>(data.Request.ToString());
                    Int32 HotelId = 0;

                    CancellationPilicyRS_newapi response = await _hotelXML.GetCancellationPolicy(payload, authData.tokenData, data.AdvancedOptions, userAgent, IP);

                    CancellationPilicyRS lstFiltOption = response.CancellationPilicyRS;
                    HotelId = response.HotelId;
                    Seach_Key = payload.SearchKey;

                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);
                        }
                        else
                        {

                            lstFiltOption.CancellationRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            lstFiltOption.CancellationRS.EssentialInformation = lstFiltOption.CancellationRS.EssentialInformation + EssentialInfo.Essentialinformation;

                            timer.Stop();

                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        lstFiltOption.Error.Add(error);
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                CancellationPilicyRS RS = new CancellationPilicyRS();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("CancellationPolicyMulti_9")]
        public async Task<IActionResult> CancellationPolicyMulti_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only

                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());
                    Int32 HotelId = 0;

                    CancellationPolicyRS_Multi_newapi response = await _hotelXML.GetCancellationPolicyMulti(payload, authData.tokenData, data.AdvancedOptions, userAgent, IP);

                    CancellationPolicyRS_Multi lstFiltOption = response.CancellationPolicyRS_Multi;
                    HotelId = response.HotelId;
                    Seach_Key = payload.Searchkey;

                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);
                        }
                        else
                        {
                            lstFiltOption.Currency = authData.tokenData.CreditLimitCurrencyName;
                            for (int x = 0; x < lstFiltOption.CancellationRS.Count; x++)
                            {
                                lstFiltOption.CancellationRS[x].EssentialInformation = lstFiltOption.CancellationRS[x].EssentialInformation + EssentialInfo.Essentialinformation;
                            }

                            timer.Stop();

                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        lstFiltOption.Error.Add(error);
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                CancellationPolicyRS_Multi RS = new CancellationPolicyRS_Multi();
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancellationPolicy", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("ReCheck")]
        public async Task<IActionResult> ReCheck(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            //string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only

                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //CancellationPolicyRS_Multi_v1 lstFiltOption = _hotelXML.PolicyWithPrice(payload, TokenData, data.AdvancedOptions);
                    CancellationPolicyRS_Multi_v1_newapi response = await _hotelXML.PolicyWithPrice(payload, authData.tokenData, data.AdvancedOptions, userAgent, IP);
                    Seach_Key = payload.Searchkey;

                    CancellationPolicyRS_Multi_v1 lstFiltOption = response.CancellationPolicyRS_Multi_v1;
                    HotelId = response.HotelId;

                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200, null, timer.ElapsedMilliseconds);
                        }
                        else
                        {
                            bool IsNotAvailable = false;
                            for (int i = 0; i < lstFiltOption.ReCheckRS.Count; i++)
                            {
                                if (lstFiltOption.ReCheckRS[i].BookingStatus == "NotAvailable")
                                {
                                    IsNotAvailable = true;
                                }
                            }
                            if (IsNotAvailable)
                            {
                                if (authData.tokenData.IsFromAPI)
                                    lstFiltOption.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                                else
                                    lstFiltOption.Currency = authData.tokenData.SiteCurrency;

                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200, null, timer.ElapsedMilliseconds);
                            }
                            else
                            {
                                for (int x = 0; x < lstFiltOption.ReCheckRS.Count; x++)
                                {
                                    lstFiltOption.ReCheckRS[x].EssentialInformation = lstFiltOption.ReCheckRS[x].EssentialInformation + EssentialInfo.Essentialinformation;

                                }

                                if (authData.tokenData.IsFromAPI)
                                    lstFiltOption.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                                else
                                    lstFiltOption.Currency = authData.tokenData.SiteCurrency;

                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200, null, timer.ElapsedMilliseconds);
                            }
                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        lstFiltOption.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.ErrorNew(ex, _LogAPI.Recheck);
                CancellationPolicyRS_Multi_v1 RS = new CancellationPolicyRS_Multi_v1();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "ReCheck", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);
            }

        }

        [HttpPost]
        [Route("PreBook_9")]
        public async Task<IActionResult> PreBook_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    PreBookResponse_newapi response = await _hotelXML.PreBook_9(payload, authData.tokenData, data.AdvancedOptions, JsonRQ, userAgent, IP, MainURL);

                    Seach_Key = payload.Searchkey;

                    PreBookResponse RS = response.PreBookResponse;

                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = response.DataforLog.provider;
                        data.AdvancedOptions.hotelid = response.DataforLog.hotelid;
                        data.AdvancedOptions.HotelName = response.DataforLog.hotelname;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                _ = _exception.Error(x);
                            }

                            if (RS.PreBookRS != null)
                            {
                                timer.Stop();
                                RS.PreBookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);
                            }
                            else
                            {
                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);

                            }

                        }
                        else
                        {
                            RS.PreBookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                PreBookResponse RS = new PreBookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("Book_9")]
        public async Task<IActionResult> Book_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (data.IsMobile == 1)
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    DataforLog log = new DataforLog();
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    BookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<BookRequest>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    BookResponse_newapi response = await _hotelXML.Book(payload, authData.tokenData, data.AdvancedOptions, JsonRQ, userAgent, IP, MainURL);
                    BookResponse RS = response.BookResponse;
                    log = response.DataforLog;

                    string Ref = "";
                    string IntRef = "";
                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = log.provider;
                        data.AdvancedOptions.hotelid = log.hotelid;
                        data.AdvancedOptions.HotelName = log.hotelname;
                        Ref = RS.BookRS.ReferenceNo;
                        IntRef = RS.BookRS.InternalReference;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                _ = _exception.Error(x);
                            }
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.SearchKey, EntryTime, Ref, IntRef, "", 200);

                        }
                        else
                        {
                            RS.BookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.SearchKey, EntryTime, Ref, IntRef, "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + ".");
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.SearchKey, EntryTime, Ref, IntRef, "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.ErrorBooking(ex, "001");
                BookResponse RS = new BookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("Book_newversion")]
        public async Task<IActionResult> Book_newversion(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            //string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (data.IsMobile == 1)
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book_newversion", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    DataforLog log = new DataforLog();
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    BookRequest_newversion payload = Newtonsoft.Json.JsonConvert.DeserializeObject<BookRequest_newversion>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    BookResponse_newversion RS = await _hotelXML.Book_newversion(payload, authData.tokenData, data.AdvancedOptions, JsonRQ, userAgent, IP, MainURL);
                    //BookResponse RS = response.BookResponse;
                    //log = response.DataforLog;

                    string Ref = "";
                    string IntRef = "";
                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = payload.userCart.UserCartDetails[0].SupplierName;
                        data.AdvancedOptions.hotelid = payload.userCart.ServiceId;
                        data.AdvancedOptions.HotelName = payload.userCart.ServiceName;
                        Ref = payload.BookRQ.InternalReference;
                        IntRef = payload.BookRQ.InternalReference;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                _ = _exception.Error(x);
                            }
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "Book_newversion", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.SearchKey, EntryTime, Ref, IntRef, "", 200);

                        }
                        else
                        {
                            //RS.BookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "Book_newversion", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.SearchKey, EntryTime, Ref, IntRef, "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + ".");
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "Book_newversion", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.SearchKey, EntryTime, Ref, IntRef, "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book_newversion", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.ErrorBooking(ex, "001");
                BookResponse RS = new BookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "Book_newversion", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("CancelBooking_newversion")]
        public async Task<IActionResult> CancelBooking_newversion(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            //string IP = _platformService.GetIPAddress();
            string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (data.IsMobile == 1)
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book_newversion", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    DataforLog log = new DataforLog();
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    CancelBookingRequest_new payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CancelBookingRequest_new>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    CancelBookingResponse_newversion1 RS = await _hotelXML.Cancel_newversion(payload, authData.tokenData, data.AdvancedOptions, JsonRQ, userAgent, IP, MainURL);
                    //BookResponse RS = response.BookResponse;
                    //log = response.DataforLog;

                    string Ref = payload.CancelRQ.ReferenceNo;
                    string IntRef = "";
                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = payload.CancelRQ.Provider;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                _ = _exception.Error(x);
                            }
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "CancelBooking_newversion", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, "", EntryTime, Ref, IntRef, "", 200);

                        }
                        else
                        {
                            //RS.BookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "CancelBooking_newversion", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, "", EntryTime, Ref, IntRef, "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + ".");
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "CancelBooking_newversion", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, "", EntryTime, Ref, IntRef, "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancelBooking_newversion", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.ErrorBooking(ex, "001");
                BookResponse RS = new BookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancelBooking_newversion", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("BookingDetail_9")]
        public async Task<IActionResult> BookingDetail_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            //string IP = _platformService.GetIPAddress();
            string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    DataforLogBD log = new DataforLogBD();
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    BookingDetailRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingDetailRequest>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    BookingDetailResponse_newapi response = await _hotelXML.BookingDetail(payload, authData.tokenData, data.AdvancedOptions, JsonRQ, userAgent, IP, MainURL);
                    BookingDetailResponse RS = response.BookingDetailResponse;
                    log = response.DataforLogBD;

                    string Ref = "";
                    string IntRef = "";
                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = log.provider;
                        data.AdvancedOptions.hotelid = log.hotelid;
                        data.AdvancedOptions.HotelName = log.hotelname;
                        Ref = log.Ref;
                        IntRef = log.IntRef;

                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", 200);

                        }
                        else
                        {
                            RS.BookingDetailRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.BookingDetailRQ.ReferenceNo, EntryTime, Ref, IntRef, "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                BookingDetailResponse RS = new BookingDetailResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "BookingDetail", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("CheckHotelCancellationCharges_9")]
        public async Task<IActionResult> CheckHotelCancellationCharges_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    CheckChargesRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckChargesRequest>(data.Request.ToString());
                    CheckChargesResponse lstFiltOption = await _hotelXML.CheckHotelCancellationCharges(payload, authData.tokenData, data.AdvancedOptions);

                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", 200);

                        }
                        else if (lstFiltOption.CheckHotelCancellationChargesRS.HotelOption.HotelRooms.Count == 0)
                        {
                            timer.Stop();
                            //lstFiltOption.Error.Add(HotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NoPermission_Cancel)); // comment on 260225
                            lstFiltOption.Error.Add(_hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.DUPLICATION, ErrorDescription.Booking_Cancelled));//#55391
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", 200);

                        }
                        else
                        {
                            lstFiltOption.CheckHotelCancellationChargesRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        lstFiltOption.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, payload.CheckHotelCancellationChargesRQ.ReferenceNo, EntryTime, payload.CheckHotelCancellationChargesRQ.ReferenceNo, payload.CheckHotelCancellationChargesRQ.InternalReference, "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                CheckChargesResponse RS = new CheckChargesResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CheckHotelCancellationCharges", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("CancelBooking_9")]
        public async Task<IActionResult> CancelBooking_9(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (data.IsMobile == 1)
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    string PNRNo = "";

                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    CancelBookingRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<CancelBookingRequest>(data.Request.ToString());
                    CancelBookingResponse_newapi response = await _hotelXML.CancelBooking(payload, authData.tokenData, data.AdvancedOptions, userAgent, IP, MainURL);
                    CancelBookingResponse lstFiltOption = response.CancelBookingResponse;
                    PNRNo = response.PNRNo;

                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", 200);

                        }
                        else if (lstFiltOption.CancelRS.HotelOption.HotelRooms.Count == 0)
                        {
                            timer.Stop();
                            lstFiltOption.Error.Add(_hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NoPermission_Cancel));
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", 200);

                        }
                        else
                        {
                            lstFiltOption.CancelRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", 200);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        timer.Stop();
                        lstFiltOption.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, PNRNo, EntryTime, PNRNo, payload.CancelRQ.InternalReference, "", 500);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                CancelBookingResponse RS = new CancelBookingResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "CancelBooking", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }

        [HttpPost]
        [Route("ReBook_PruvoMarketPlace")]
        public async Task<IActionResult> ReBook_PruvoMarketPlace(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;

                if (data.AdvancedOptions.Currency.ToLower() != authData.tokenData.SiteCurrency.ToLower())
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, data.AdvancedOptions.Currency + " Currency Not Support.", "application/json", data, "401", authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }


                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    Class_Pruvo.ReBook_PruvoMarketPlace payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Class_Pruvo.ReBook_PruvoMarketPlace>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    BookResponse RS = await _hotelXML.ReBookPruvoMarketPlace(payload, authData.tokenData, data.AdvancedOptions, IP);

                    string Ref = "";
                    string IntRef = "";
                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = "PurvoMarketplace";
                        data.AdvancedOptions.hotelid = 0;
                        data.AdvancedOptions.HotelName = "";
                        Ref = RS.BookRS.ReferenceNo;
                        IntRef = RS.BookRS.InternalReference;
                    }
                    catch (Exception ex)
                    {

                        _ = _exception.ErrorNew(ex, _LogAPI.Book);
                    }



                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                _ = _exception.ErrorNew(x, _LogAPI.Book);
                            }
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.pruvoItinerary, EntryTime, Ref, IntRef, "", 200);

                        }
                        else
                        {
                            RS.BookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.pruvoItinerary, EntryTime, Ref, IntRef, "", 200);
                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, payload.pruvoItinerary, EntryTime, Ref, IntRef, "", 500);

                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401);

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                PreBookResponse RS = new PreBookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "Book", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500);
            }

        }
        #endregion

        #region RedisCache_oth
        [HttpPost]
        [Route("RedisCache_oth")]
        public async Task<IActionResult> RedisCache_oth(CacheSearchpara SearchPara) //Added on 29-APR-2025
        {
            try
            {
                bool Tokendata = await _hotelXML.GetRedisCache_oth(SearchPara, SearchPara.HotelID, SearchPara.checkInDate, SearchPara.checkOutDate, SearchPara.CityID);

                if (Tokendata)
                {
                    return new ObjectResult(new { Tokendata })
                    {
                        StatusCode = 200
                    };
                }
                else
                {
                    return new ObjectResult(new { Tokendata })
                    {
                        StatusCode = 500
                    };
                }
            }
            catch (Exception x)
            {
                return new ObjectResult(false)
                {
                    StatusCode = 500
                };
            }


        }
        #endregion

        #region Pool Action
        //[HttpPost]
        //[Route("poolstart")]
        //public async Task<IActionResult> poolstart(PoolOperation poolOperation) ///Added on 24-JULY-2025
        //{
        //    string URL = poolOperation.URL;
        //    string operationType = poolOperation.operationType;
        //    bool isHealthy = true;
        //    try
        //    {
        //        string hostName = System.Net.Dns.GetHostName();

        //        if (hostName == "API1" || hostName == "API2" || hostName == "API3" || hostName == "API4" || hostName == "API5" || hostName == "API6" || hostName == "API7")
        //        {
        //            switch (operationType.ToLower())
        //            {
        //                case "start":
        //                    await _hotelXML.StartAppPool(URL);
        //                    break;
        //                case "stop":
        //                    await _hotelXML.StopAppPool(URL);
        //                    break;
        //                case "recycle":
        //                    await _hotelXML.RecycleAppPool(URL);
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //        else
        //        {
        //            switch (operationType.ToLower())
        //            {
        //                case "start":
        //                    await _hotelXML.StartAppPool(URL);
        //                    break;
        //                case "stop":
        //                    await _hotelXML.StopAppPool(URL);
        //                    break;
        //                case "recycle":
        //                    await _hotelXML.RecycleAppPool(URL);
        //                    break;
        //                default:
        //                    break;
        //            }

        //        }
        //    }
        //    catch (Exception x)
        //    {
        //        isHealthy = false;
        //    }

        //    if (isHealthy)
        //    {
        //        return new ObjectResult("ok")
        //        {
        //            StatusCode = 200
        //        };
        //    }
        //    else
        //    {
        //        return new ObjectResult("unhealthy")
        //        {
        //            StatusCode = 500
        //        };
        //    }
        //}

        //public class PoolOperation
        //{
        //    public string URL { get; set; }
        //    public string operationType { get; set; }
        //}
        #endregion

        #region New Version Code
        [HttpPost]
        [Route("PreBook")]
        public async Task<IActionResult> PreBook(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DateTime EntryTime = DateTime.Now;
            TokenData TokenData = new TokenData();
            string Seach_Key = "";

            var headerValue = Request.Headers["Authorization"].ToString();

            //var headerValue = Request.Headers["Authorization"].ToString();

            string token = headerValue.StartsWith("Bearer ")
                ? headerValue.Substring("Bearer ".Length).Trim()
                : headerValue;

            string IP = _platformService.GetIPAddress();
            //string IP = Convert.ToString(_userContextAccessor.ClientStats.ClientIp);
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    data.AdvancedOptions.Currency = authData.tokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    string JsonRQ = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    PreBookRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookRequest>(data.Request.ToString());
                    Int32 HotelId = 0;

                    //PreBookResponse RS = _xconnect.PreBook(payload, TokenData, data.AdvancedOptions, ref log, JsonRQ);//#MongoDB
                    PreBookResponse_newapi response = await _hotelXML.PreBook(payload, authData.tokenData, data.AdvancedOptions, JsonRQ, userAgent, IP, MainURL, token);

                    Seach_Key = payload.Searchkey;

                    PreBookResponse RS = response.PreBookResponse;

                    try
                    {
                        data.AdvancedOptions.SearchByOneID = true;
                        data.AdvancedOptions.Provider = response.DataforLog.provider;
                        data.AdvancedOptions.hotelid = response.DataforLog.hotelid;
                        data.AdvancedOptions.HotelName = response.DataforLog.hotelname;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }


                    if (RS != null)
                    {
                        if (RS.Error.Count > 0)
                        {
                            string Error_Code = ErrorCode.SomeError;
                            try
                            {
                                Error_Code = RS.Error[0].code;
                            }
                            catch (Exception x)
                            {
                                _ = _exception.Error(x);
                            }

                            if (RS.PreBookRS != null)
                            {
                                timer.Stop();
                                if (authData.tokenData.IsFromAPI)
                                    RS.PreBookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                                else
                                    RS.PreBookRS.Currency = authData.tokenData.SiteCurrency;

                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200, null, timer.ElapsedMilliseconds);
                            }
                            else
                            {
                                timer.Stop();
                                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), Error_Code, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200, null, timer.ElapsedMilliseconds);

                            }

                        }
                        else
                        {
                            if (authData.tokenData.IsFromAPI)
                                RS.PreBookRS.Currency = authData.tokenData.CreditLimitCurrencyName;
                            else
                                RS.PreBookRS.Currency = authData.tokenData.SiteCurrency;

                            timer.Stop();
                            return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 200, null, timer.ElapsedMilliseconds);

                        }
                    }
                    else
                    {
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                        timer.Stop();
                        RS.Error.Add(error);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, Convert.ToString(HotelId), "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, "401", "application/json", data, "401", authData.tokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", IP, "", "", "", 0, 0, 0, "", EntryTime, "", "", "", 401, null, timer.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                PreBookResponse RS = new PreBookResponse();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError + "_");
                RS.Error.Add(error);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, RS, "application/json", data, RS, TokenData, "PreBook", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", IP, "", "", "", 0, 0, 0, Seach_Key, EntryTime, "", "", "", 500, null, timer.ElapsedMilliseconds);
            }

        }



        #endregion

        #region MobileTeamNewVersion

        //GetStaticDataByCity take from Exesting project
        [Route("GetCitiesWithHotel")]
        [HttpPost]
        public async Task<IActionResult> GetCitiesWithHotel(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                //if (Convert.ToBoolean(authData.tokenData.AgentId))
                //{
                StaticDataCity_Auto payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity_Auto>(data.Request.ToString());

                // List<StaticData> lstFiltOption = await _hotelXML.GetCitiesWithHotel(payload.CityName);
                DataTable lstFiltOption = await _hotelXML.GetCitiesWithHotel(payload.CityName);
                if (lstFiltOption != null)
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "GetCitiesWithHotel", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.CityName.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                }
                else
                {
                    timer.Stop();
                    error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetCitiesWithHotel", true, timer.Elapsed.ToString(), ErrorCode.InternalError, payload.CityName.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                }
                //}
                //else
                //{
                //    timer.Stop();
                //    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetCitiesWithHotel", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                //}
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetStaticDataByCity", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetStaticDataByCity")]
        [HttpPost]
        public async Task<IActionResult> GetStaticDataByCity(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                //if (Convert.ToBoolean(authData.tokenData.AgentId))
                //{
                StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                List<StaticData> lstFiltOption = await _hotelXML.GetStaticDataByCity(Convert.ToInt32(payload.CityId), Convert.ToString(payload.LocationId), payload.Type);
                if (lstFiltOption != null)
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "GetStaticDataByCity", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                }
                else
                {
                    timer.Stop();
                    error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetStaticDataByCity", true, timer.Elapsed.ToString(), ErrorCode.InternalError, payload.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                }
                //}
                //else
                //{
                //    timer.Stop();
                //    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetStaticDataByCity", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                //}
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetStaticDataByCity", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetHotelStaticDataOptimize")]
        [HttpPost]
        public async Task<IActionResult> GetHotelStaticDataOptimize(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetHotelStaticDataOptimize", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetHotelStaticDataOptimize", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetHotelStaticDataOptimize", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetHotelStaticDataOptimize", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }


        [Route("gethotelstaticdatalistsuboptimize_v1_Address")]
        [HttpPost]
        public async Task<IActionResult> gethotelstaticdatalistsuboptimize_v1_Address(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.gethotelstaticdatalistsuboptimize_v1_Address(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "gethotelstaticdatalistsuboptimize_v1_Address", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "gethotelstaticdatalistsuboptimize_v1_Address", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "gethotelstaticdatalistsuboptimize_v1_Address", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "gethotelstaticdatalistsuboptimize_v1_Address", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetHotelStaticDataOptimizeImage")]
        [HttpPost]
        public async Task<IActionResult> GetHotelStaticDataOptimizeImage(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimizeImage(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetHotelStaticDataOptimizeImage", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetHotelStaticDataOptimizeImage", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetHotelStaticDataOptimizeImage", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetHotelStaticDataOptimizeImage", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("ForPOIListing")]
        [HttpPost]
        public async Task<IActionResult> ForPOIListing(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.ForPOIListing(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "ForPOIListing", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "ForPOIListing", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "ForPOIListing", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "ForPOIListing", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("Filtering")]
        [HttpPost]
        public async Task<IActionResult> Filtering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.Filtering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "Filtering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "Filtering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "Filtering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "Filtering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("ForNeighbourHoodFiltering")]
        [HttpPost]
        public async Task<IActionResult> ForNeighbourHoodFiltering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.ForNeighbourHoodFiltering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "ForNeighbourHoodFiltering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "ForNeighbourHoodFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "ForNeighbourHoodFiltering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "ForNeighbourHoodFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("FotPropertyThemesFiltering")]
        [HttpPost]
        public async Task<IActionResult> FotPropertyThemesFiltering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.FotPropertyThemesFiltering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "FotPropertyThemesFiltering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "FotPropertyThemesFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "FotPropertyThemesFiltering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "FotPropertyThemesFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }



        [Route("FotPropertyCategoryFiltering")]
        [HttpPost]
        public async Task<IActionResult> FotPropertyCategoryFiltering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.FotPropertyCategoryFiltering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "FotPropertyCategoryFiltering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "FotPropertyCategoryFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "FotPropertyCategoryFiltering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "FotPropertyCategoryFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("FotPropertyAmenitiesFiltering")]
        [HttpPost]
        public async Task<IActionResult> FotPropertyAmenitiesFiltering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.FotPropertyAmenitiesFiltering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "FotPropertyAmenitiesFiltering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "FotPropertyAmenitiesFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "FotPropertyAmenitiesFiltering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "FotPropertyAmenitiesFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("FotBedpreferenceFiltering")]
        [HttpPost]
        public async Task<IActionResult> FotBedpreferenceFiltering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.FotBedpreferenceFiltering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "FotBedpreferenceFiltering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "FotBedpreferenceFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "FotBedpreferenceFiltering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "FotBedpreferenceFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("FotPropertyChainFiltering")]
        [HttpPost]
        public async Task<IActionResult> FotPropertyChainFiltering(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.FotPropertyChainFiltering(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "FotPropertyChainFiltering", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "FotPropertyChainFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "FotPropertyChainFiltering", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "FotPropertyChainFiltering", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyCategory")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyCategory(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyCategory(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyCategory", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyCategory", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyCategory", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyCategory", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyPaymentCollectionInfo")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyPaymentCollectionInfo(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyPaymentCollectionInfo(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyPaymentCollectionInfo", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyPaymentCollectionInfo", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyPaymentCollectionInfo", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyPaymentCollectionInfo", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyFees")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyFees(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyFees(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyFees", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyFees", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyFees", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyFees", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyPolicies")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyPolicies(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyPolicies(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyPolicies", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyPolicies", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyPolicies", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyPolicies", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyAttribute")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyAttribute(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyAttribute(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyAttribute", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyAttribute", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyAttribute", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyAttribute", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyAmenities")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyAmenities(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyAmenities(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyAmenities", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyAmenities", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyAmenities", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyAmenities", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyOnsitePayments")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyOnsitePayments(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyOnsitePayments(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyOnsitePayments", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyOnsitePayments", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyOnsitePayments", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyOnsitePayments", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyRoomDescription")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyRoomDescription(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyRoomDescription(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyRoomDescription", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyRoomDescription", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyRoomDescription", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyRoomDescription", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyRoomAmenities")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyRoomAmenities(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyRoomAmenities(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyRoomAmenities", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyRoomAmenities", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyRoomAmenities", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyRoomAmenities", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GETPropertyRoomImages")]
        [HttpPost]
        public async Task<IActionResult> GETPropertyRoomImages(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GETPropertyRoomImages(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GETPropertyRoomImages", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GETPropertyRoomImages", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GETPropertyRoomImages", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GETPropertyRoomImages", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GETPropertyRoomBedGroups")]
        [HttpPost]
        public async Task<IActionResult> GETPropertyRoomBedGroups(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GETPropertyRoomBedGroups(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GETPropertyRoomBedGroups", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GETPropertyRoomBedGroups", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GETPropertyRoomBedGroups", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GETPropertyRoomBedGroups", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GETPropertyRoomBedGroupsConfiguration")]
        [HttpPost]
        public async Task<IActionResult> GETPropertyRoomBedGroupsConfiguration(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GETPropertyRoomBedGroupsConfiguration(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GETPropertyRoomBedGroupsConfiguration", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GETPropertyRoomBedGroupsConfiguration", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GETPropertyRoomBedGroupsConfiguration", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GETPropertyRoomBedGroupsConfiguration", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GETPropertyRoomArea")]
        [HttpPost]
        public async Task<IActionResult> GETPropertyRoomArea(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GETPropertyRoomArea(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GETPropertyRoomArea", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GETPropertyRoomArea", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GETPropertyRoomArea", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GETPropertyRoomArea", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GETPropertyRoomViews")]
        [HttpPost]
        public async Task<IActionResult> GETPropertyRoomViews(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GETPropertyRoomViews(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GETPropertyRoomViews", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GETPropertyRoomViews", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GETPropertyRoomViews", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GETPropertyRoomViews", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GETPropertyRoomOccupancyMaxAllowed")]
        [HttpPost]
        public async Task<IActionResult> GETPropertyRoomOccupancyMaxAllowed(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GETPropertyRoomOccupancyMaxAllowed(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GETPropertyRoomOccupancyMaxAllowed", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GETPropertyRoomOccupancyMaxAllowed", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GETPropertyRoomOccupancyMaxAllowed", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GETPropertyRoomOccupancyMaxAllowed", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyRoomOccupancyAgeCategories")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyRoomOccupancyAgeCategories(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyRoomOccupancyAgeCategories(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyRoomOccupancyAgeCategories", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyRoomOccupancyAgeCategories", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyRoomOccupancyAgeCategories", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyRoomOccupancyAgeCategories", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyDescriptions")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyDescriptions(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyDescriptions(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyDescriptions", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyDescriptions", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyDescriptions", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyDescriptions", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyThemes")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyThemes(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyThemes(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyThemes", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyThemes", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyThemes", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyThemes", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyChain")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyChain(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyChain(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyChain", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyChain", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyChain", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyChain", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertyBrand")]
        [HttpPost]
        public async Task<IActionResult> GetPropertyBrand(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertyBrand(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertyBrand", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertyBrand", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertyBrand", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertyBrand", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetPropertySpokenLanguages")]
        [HttpPost]
        public async Task<IActionResult> GetPropertySpokenLanguages(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    //StaticDataCity payload = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticDataCity>(data.Request.ToString());

                    HotelSync para = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSync>(data.Request.ToString());


                    // List<StaticData> lstFiltOption = await _hotelXML.GetHotelStaticDataOptimize(Convert.ToInt32(payload.CityId), Convert.ToInt32(payload.LocationId), payload.Type);

                    //DataSet HotelDetailList = await _hotelXML.GetHotelStaticDataOptimize(para);
                    DataSet HotelDetailList = await _hotelXML.GetPropertySpokenLanguages(para);
                    if (HotelDetailList != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, HotelDetailList, "application/json", data, HotelDetailList, authData.tokenData, "GetPropertySpokenLanguages", true, timer.Elapsed.ToString(), ErrorCode.NoError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", authData.tokenData, "GetPropertySpokenLanguages", true, timer.Elapsed.ToString(), ErrorCode.InternalError, para.CityId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetPropertySpokenLanguages", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetPropertySpokenLanguages", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }
        #endregion MobileTeamNewVersion

        [Route("RemoveAgentWiseSupplier")]
        [HttpGet]
        public async Task<IActionResult> RemoveAgentWiseSupplier()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            try
            {
                int AgentId = await _authenticationTokenAgent.Authenticate_new();

                if (Convert.ToBoolean(AgentId))
                {
                    String lstFiltOption = await _hotelXML.RemoveAgentWiseSupplier(AgentId);
                    return Ok(lstFiltOption);
                }
                else
                {
                    timer.Stop();
                    return NotFound(StatusCodes.Status401Unauthorized);
                }


            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [Route("GetRangeForAPI")]
        [HttpPost]
        public async Task<IActionResult> GetRangeForAPI(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    GetRangeForAPI payload = Newtonsoft.Json.JsonConvert.DeserializeObject<GetRangeForAPI>(data.Request.ToString());
                    List<GetRange> lstFiltOption = await _hotelXML.GetRangeForAPI(payload, authData.tokenData.AgentId);
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    timer.Stop();
                    return NotFound(StatusCodes.Status401Unauthorized);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }

        [Route("GetAgentListApiOutLogs")]
        [HttpPost]
        public async Task<IActionResult> GetAgentListApiOutLogs(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    ApiOutLogs payload = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiOutLogs>(data.Request.ToString());
                    List<Agentlistlogs> lstFiltOption = await _hotelXML.GetAgentListApiOutLogs(payload, authData.tokenData.AgentId);
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    timer.Stop();
                    return NotFound(StatusCodes.Status401Unauthorized);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }
        [Route("GetApiOutLogs")]
        [HttpPost]
        public async Task<IActionResult> GetApiOutLogs(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                //if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    ApiRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRequest>(data.Request.ToString());
                    List<Supplierlogs> lstFiltOption = await _hotelXML.GetApiOutLogs(payload);
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
                }
                //else
                //{
                //    timer.Stop();
                //    return NotFound(StatusCodes.Status401Unauthorized);
                //}
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }

        #region agoda client

        bool EnterLogs = true;

        [HttpPost]
        [Route("/v1.9/hotelSearch")]  
        [Route("/v1.9/precheck")]  
        public async Task<IActionResult> hotelSearch(AgodaSearchPara commonRQ)

        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData tokenData = new TokenData();
            CommonRequest data = new CommonRequest();
            data.AdvancedOptions = new AdvancedOptions();
            string requestName = "availability";
            FilterUsed filterUsed = new FilterUsed();
            filterUsed.ratePlanSearch = false;
            filterUsed.OriginalcheckOut = commonRQ.checkOut;
            filterUsed.OriginalcheckIn = commonRQ.checkIn;
            int _NoofRoom = commonRQ.occupancy.rooms;
            string CmName = "";
            string ChainCode_Agoda = "";
            if (!String.IsNullOrEmpty(commonRQ.ratePlanCode))
            {
                requestName = "Precheck";
                filterUsed.ratePlanCode = commonRQ.ratePlanCode;
                filterUsed.totalPrice = commonRQ.totalPrice;
                filterUsed.ratePlanSearch = true;
                string[] arraysearchpra = commonRQ.ratePlanCode.Split(new String[] { "$$$" }, StringSplitOptions.None);
                switch (arraysearchpra[arraysearchpra.Length - 1])
                {
                    case "177":
                        CmName = "rategain";
                        break;
                    case "182":
                        CmName = "travelclick";
                        break;
                    case "112":
                        CmName = "siteminder";
                        break;
                    default:
                        break;
                }

                try
                {
                    string ratePlanCode = "";
                    getrateplan.Root rateplan = new getrateplan.Root();
                    rateplan = Newtonsoft.Json.JsonConvert.DeserializeObject<getrateplan.Root>(commonRQ.metadata);

                    ratePlanCode = rateplan.token;
                    byte[] decodedBytes = Convert.FromBase64String(ratePlanCode);
                    string decoded_ratePlanCode = Encoding.UTF8.GetString(decodedBytes);
                    string RoomRatePlanCode = decoded_ratePlanCode;
                    string[] spitid1 = null;
                    if (!string.IsNullOrEmpty(ratePlanCode))
                    {
                        spitid1 = RoomRatePlanCode.Split(new String[] { "^^" }, StringSplitOptions.None);
                    }

                    ChainCode_Agoda = (spitid1 != null && spitid1.Length > 9) ? Convert.ToString(spitid1[9]) : string.Empty;
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }

            }
            try
            {
                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetUserPlatform(IP);

                //Validation NoofNight
                if ((Convert.ToDateTime(commonRQ.checkOut).Date - (Convert.ToDateTime(commonRQ.checkIn).Date)).TotalDays > 30)
                {
                    erroragoda = _hotelXML.GetAgodaErrorMsg("Request_Error", _keySettings.AgodaVersion, "NoofNights between 1 and 30");

                    #region Elastic
                    try
                    {
                        _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                        {
                            Action = requestName,
                            HttpStatusCode = Convert.ToInt32(404),
                            Stage = requestName == "availability" ? "Search" : "Recheck",
                            ElapsedTime = timer.ElapsedMilliseconds,
                            Description = erroragoda.responseStatus,
                            Errorcode = "NOT_SUCCESSFULL",
                            source = "APIOut",
                            AgentName = "Agoda",
                            ActualDescription = erroragoda.remark,
                            srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                            Channelcode = ChainCode_Agoda
                        });
                    }
                    catch (Exception exx)
                    {
                        await _exception.Error(exx);
                    }
                    #endregion
                    //uncmnt on live
                    //return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.Unauthorized, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.RequestError, "0");
                    return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", HttpStatusCode.NotFound, "", timer.ElapsedMilliseconds);

                }
                //--end
                //commonRQ.token = WebConfigurationManager.AppSettings["agoda_token"];
                commonRQ.token = _keySettings.agoda_apikey;
                data.Request = commonRQ.ToString();
                // if (AgodaAuthentication(ref tokenData, ref data))
                //AuthData authData = await _authenticationTokenAgent.Authenticate(tokenData, data, IP, userAgent);

                string Headrertoken = "";
                    
                AuthData authData = await _authenticationTokenAgent.AgodaAuthentication(tokenData, data);


                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    HotelSearchParameter payload = new HotelSearchParameter();
                    payload.CityID = 0;
                    _hotelXML.PrepareForAgodaCommonSearch(ref payload, ref data, ref erroragoda, authData.tokenData, requestName, filterUsed, commonRQ);

                    if (erroragoda.responseStatus != null)
                    {
                        // return Request.CreateResponseCompressed(HttpStatusCode.OK, erroragoda, "application/json", data, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.CityID.ToString());
                        return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", HttpStatusCode.NotFound, "", timer.ElapsedMilliseconds); //28052026

                    }

                    //AvailHotelList finalResponse = HotelXML.HotelSearchCommon(payload, tokenData, data.AdvancedOptions, requestName, CmName, commonRQ.metadata);
                    AvailHotelList finalResponse = await _hotelXML.HotelSearchCommon(payload, authData.tokenData, data.AdvancedOptions, timer, requestName, CmName, commonRQ.metadata);

                    string SearchKeyar = payload.SearchKey ?? string.Empty;
                    if (!filterUsed.ratePlanSearch)
                    {
                        Stopwatch timerforagodaconvert = new Stopwatch();
                        timerforagodaconvert.Start();
                        AgodaListResponse lstFiltOption = new AgodaListResponse();
                        //File.WriteAllText(Path.Combine("D:\\Karnav\\Agoda API", "WriteFile_HotelResult.txt"), Convert.ToString(finalResponse.AvailabilityRS.HotelResult.Count));
                        if (finalResponse.AvailabilityRS != null && finalResponse.AvailabilityRS.HotelResult.Count > 0)
                        {

                            //lstFiltOption = finalResponse.ConvertToFinalResponseListForAgoda(filterUsed, Convert.ToString(tokenData.apiCurrency.CurrencyShortname), requestName, _NoofRoom); //convert to final response
                            //lstFiltOption = await _hotelXML.ConvertToFinalResponseListForAgoda(finalResponse,filterUsed, Convert.ToString(tokenData.apiCurrency.CurrencyShortname), requestName, _NoofRoom); //convert to final response
                            lstFiltOption = await _hotelXML.ConvertToFinalResponseListForAgoda(finalResponse, filterUsed, authData.tokenData.apiCurrency.CurrencyShortname, requestName, _NoofRoom); //needtochange
                        }
                        timerforagodaconvert.Stop();
                        timer.Stop();
                        if (lstFiltOption != null)
                        {
                            if (lstFiltOption.hotels == null)
                            {
                                if (filterUsed.ratePlanSearch)
                                    erroragoda = _hotelXML.GetAgodaErrorMsg("NO_ALLOTMENT", "", "Given rate/allotment is not available for booking any more.");
                                //erroragoda = _hotelXML.GetErrorMsg("NO_ALLOTMENT", version.Version, "Given rate/allotment is not available for booking any more.");
                                else
                                    erroragoda = _hotelXML.GetAgodaErrorMsg("NO_AVAILABILITY", "", "no available rooms for given criteria.");
                                // erroragoda =  _hotelXML.GetErrorMsg("NO_AVAILABILITY", version.Version, "no available rooms for given criteria.");
                                //return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0");

                                #region Elastic
                                try
                                {
                                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                    {
                                        Action = requestName,
                                        Result = lstFiltOption,
                                        HttpStatusCode = Convert.ToInt32(404),
                                        Stage = requestName == "availability" ? "Search" : "Recheck",
                                        ElapsedTime = timer.ElapsedMilliseconds,
                                        Description = erroragoda.responseStatus,
                                        Errorcode = "NOT_SUCCESSFULL",
                                        source = "APIOut",
                                        AgentName = "Agoda",
                                        ActualDescription =  erroragoda.remark,
                                        CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds,
                                        srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                                        Search_Key = SearchKeyar,
                                        Channelcode = ChainCode_Agoda
                                    });
                                }
                                catch (Exception exx)
                                {
                                    await _exception.Error(exx);
                                }
                                #endregion
                                //return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext,200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NorecordError, "0", HttpStatusCode.NotFound, SearchKeyar, timer.ElapsedMilliseconds);
                                return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", HttpStatusCode.NotFound, SearchKeyar, timer.ElapsedMilliseconds); //28052026


                            }
                            else
                            {
                                #region Elastic
                                try
                                {
                                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                    {

                                        Action = requestName,
                                        Result = lstFiltOption,
                                        HttpStatusCode = Convert.ToInt32(200),
                                        Stage = requestName == "availability" ? "Search" : "Recheck",
                                        ElapsedTime = timer.ElapsedMilliseconds,
                                        Description = "SUCCESSFULL",
                                        Errorcode = "SUCCESSFULL",
                                        source = "APIOut",
                                        AgentName = "Agoda",
                                        ActualDescription = "SUCCESSFULL",
                                        CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds,
                                        srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                                        Search_Key = SearchKeyar,
                                        Channelcode = ChainCode_Agoda
                                    });
                                }
                                catch (Exception exx)
                                {
                                    await _exception.Error(exx);
                                }
                                #endregion
                                // return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                                return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0", HttpStatusCode.OK, SearchKeyar, timer.ElapsedMilliseconds); // needtochange

                            }
                        }
                        else
                        {
                            //erroragoda = _hotelXML.GetAgodaErrorMsg("SUPPLIER_SERVER_ERROR", "", "Supplier service unavailable/server down. Need to escalate to supplier.");
                            erroragoda = _hotelXML.GetAgodaErrorMsg("NO_AVAILABILITY", "", "no available rooms for given criteria.");

                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = requestName,
                                    Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(500),
                                    Stage = requestName == "availability" ? "Search" : "Recheck",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = erroragoda.responseStatus,
                                    Errorcode = "NOT_SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription =  erroragoda.remark,
                                    CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds,
                                    srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                                    Search_Key = SearchKeyar,
                                    Channelcode = ChainCode_Agoda
                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion

                            return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200 , erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", HttpStatusCode.InternalServerError, SearchKeyar);
                        }
                    }
                    else
                    {
                        Stopwatch timerforagodaconvert2 = new Stopwatch();
                        timerforagodaconvert2.Start();
                        //AgodaMainPrecheck lstFiltOption = new AgodaMainPrecheck();
                        AgodaListResponse lstFiltOption = new AgodaListResponse();

                        if (finalResponse.AvailabilityRS != null && finalResponse.AvailabilityRS.HotelResult.Count > 0)
                        {
                            // lstFiltOption = finalResponse.ConvertToFinalResponseListForAgoda(filterUsed, Convert.ToString(tokenData.apiCurrency.CurrencyShortname), "", _NoofRoom); //convert to final response
                            // lstFiltOption = await _hotelXML.ConvertToFinalResponseListForAgoda(finalResponse,filterUsed, Convert.ToString(tokenData.apiCurrency.CurrencyShortname), "", _NoofRoom); //convert to final response
                            lstFiltOption = await _hotelXML.ConvertToFinalResponseListForAgoda(finalResponse, filterUsed, authData.tokenData.apiCurrency.CurrencyShortname, requestName, _NoofRoom); //needtochange

                        }
                        timerforagodaconvert2.Stop();
                        timer.Stop();
                        if (lstFiltOption != null)
                        {
                            if (lstFiltOption.hotels == null || lstFiltOption.hotels[0].rooms.Count == 0) //needtochange
                            {
                                if (filterUsed.ratePlanSearch)
                                    erroragoda = _hotelXML.GetAgodaErrorMsg("NO_ALLOTMENT", "", "Given rate/allotment is not available for booking any more.");
                                //error = _hotelXML.GetErrorMsg("NO_ALLOTMENT", version.Version, "Given rate/allotment is not available for booking any more.");

                                else
                                    erroragoda = _hotelXML.GetAgodaErrorMsg("NO_AVAILABILITY", "", "no available rooms for given criteria.");
                                // error = _hotelXML.GetErrorMsg("NO_AVAILABILITY", version.Version, "no available rooms for given criteria.");

                                #region Elastic
                                try
                                {
                                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                    {
                                        Action = requestName,
                                        Result = lstFiltOption,
                                        HttpStatusCode = Convert.ToInt32(404),
                                        Stage = requestName == "availability" ? "Search" : "Recheck",
                                        ElapsedTime = timer.ElapsedMilliseconds,
                                        Description = erroragoda.responseStatus,
                                        Errorcode = "NOT_SUCCESSFULL",
                                        source = "APIOut",
                                        AgentName = "Agoda",
                                        ActualDescription =  erroragoda.remark,
                                        CodeProcessingTimenew = timerforagodaconvert2.Elapsed.TotalMilliseconds,
                                        srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                                        Search_Key = SearchKeyar,
                                        Channelcode = ChainCode_Agoda
                                    });
                                }
                                catch (Exception exx)
                                {
                                    await _exception.Error(exx);
                                }
                                #endregion
                                return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", HttpStatusCode.NotFound, SearchKeyar);
                            }
                            else
                            {
                                #region Elastic
                                try
                                {
                                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                    {

                                        Action = requestName,
                                        Result = lstFiltOption,
                                        HttpStatusCode = Convert.ToInt32(200),
                                        Stage = requestName == "availability" ? "Search" : "Recheck",
                                        ElapsedTime = timer.ElapsedMilliseconds,
                                        Description = "SUCCESSFULL",
                                        Errorcode = "SUCCESSFULL",
                                        source = "APIOut",
                                        AgentName = "Agoda",
                                        ActualDescription = "SUCCESSFULL",
                                        CodeProcessingTimenew = timerforagodaconvert2.Elapsed.TotalMilliseconds,
                                        srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                                        Search_Key = SearchKeyar,
                                        Channelcode = ChainCode_Agoda
                                    });
                                }
                                catch (Exception exx)
                                {
                                    await _exception.Error(exx);
                                }
                                #endregion
                                //return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                                //return _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");

                                return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0", HttpStatusCode.OK, SearchKeyar);


                            }
                        }
                        else
                        {
                            erroragoda = _hotelXML.GetAgodaErrorMsg("SUPPLIER_SERVER_ERROR", "", "Supplier service unavailable/server down. Need to escalate to supplier.");
                            //error =  _hotelXML.GetErrorMsg("SUPPLIER_SERVER_ERROR", version.Version, "Supplier service unavailable/server down. Need to escalate to supplier.");

                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = requestName,
                                    Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(500),
                                    Stage = requestName == "availability" ? "Search" : "Recheck",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = erroragoda.responseStatus,
                                    Errorcode = "INTERNAL_ERROR",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription =  erroragoda.remark,
                                    CodeProcessingTimenew = timerforagodaconvert2.Elapsed.TotalMilliseconds,
                                    srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                                    Search_Key = SearchKeyar,
                                    Channelcode = ChainCode_Agoda
                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            //return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", HttpStatusCode.InternalServerError, SearchKeyar);
                            return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, lstFiltOption, "application/json", commonRQ, lstFiltOption, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0", HttpStatusCode.OK, SearchKeyar); //28052026

                        }
                    }
                }
                else
                {
                    erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", "", "Access credential is incorrect");
                    // return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0");
                    #region Elastic
                    try
                    {
                        _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                        {
                            Action = requestName,
                            HttpStatusCode = Convert.ToInt32(401),
                            Stage = requestName == "availability" ? "Search" : "Recheck",
                            ElapsedTime = timer.ElapsedMilliseconds,
                            Description = erroragoda.responseStatus,
                            Errorcode = "UNAUTHORIZED",
                            source = "APIOut",
                            AgentName = "Agoda",
                            ActualDescription = erroragoda.remark,
                            srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                            Channelcode = ChainCode_Agoda
                        });
                    }
                    catch (Exception exx)
                    {
                        await _exception.Error(exx);
                    }
                    #endregion
                    //return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 401, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0",HttpStatusCode.OK); // needtochange
                    return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 401, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0", HttpStatusCode.NotFound); // needtochange //28052026

                }

            }
            catch (Exception ex)
            {
                string Headrertoken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                erroragoda = _hotelXML.GetAgodaErrorMsg("SUPPLIER_SERVER_ERROR", "", "Supplier service unavailable/server down. Need to escalate to supplier.");
                #region Elastic
                try
                {
                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                    {
                        Action = requestName,
                        HttpStatusCode = Convert.ToInt32(500),
                        Stage = requestName == "availability" ? "Search" : "Recheck",
                        ElapsedTime = timer.ElapsedMilliseconds,
                        Description = erroragoda.responseStatus,
                        Errorcode = "INTERNAL_ERROR",
                        source = "APIOut",
                        AgentName = "Agoda",
                        ActualDescription =  erroragoda.remark,
                        srphotelid = requestName == "availability" ? Convert.ToInt32(commonRQ.hotels[0]) : Convert.ToInt32(commonRQ.hotel),
                        Channelcode = ChainCode_Agoda
                    });
                }
                catch (Exception exx)
                {
                    await _exception.Error(exx);
                }
                #endregion
                //return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", HttpStatusCode.InternalServerError);
                return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 200, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0", HttpStatusCode.OK, Headrertoken); //28052026

            }
            return null;
        }

        [HttpPost]
        [Route("/v1.9/booking")]
        public async Task<IActionResult> Booking(AgodaBookingRQ RQ)
        {
            Stopwatch timer = new Stopwatch();
            Stopwatch codetimer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            CommonRequest data = new CommonRequest();
            data.AdvancedOptions = new AdvancedOptions();

            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);

            //var authResult = await _authenticationTokenAgent.Authenticate(Request.Headers["Authorization"], new TokenData(), new CommonRequest());
            //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
            AuthData authData = await _authenticationTokenAgent.AgodaAuthentication(TokenData, data);

            //add try catch block
            try
            {
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    AgodaBookingRS agodaBookingRS = new AgodaBookingRS();
                    AgodaBookingErrorRS_New bookingerrorcode = new AgodaBookingErrorRS_New();
                    AgodaBookingErrorRS agodaError = new AgodaBookingErrorRS();
                    agodaBookingRS = await _hotelXML.AgodaBookingProcess(RQ, authData.tokenData, timer, codetimer);

                    if (agodaBookingRS != null)

                    {
                        //Change 5/03/20
                        if (agodaBookingRS.remark == "booking already exists")
                        {
                            agodaError.responseStatus = "SUCCESS";
                            agodaError.remark = agodaBookingRS.remark;
                            agodaError.referenceId = agodaBookingRS.referenceId;
                            agodaError.bookingReferenceId = agodaBookingRS.bookingReferenceId;
                            timer.Stop();
                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "Booking",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(404),
                                    Stage = "Book",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = "DUPLICATION_ERROR",
                                    Errorcode = "DUPLICATION_ERROR",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = agodaError.remark,
                                    //srphotelid = Convert.ToInt32(RQ.hotel),
                                    srphotelid = Convert.ToInt32(string.IsNullOrWhiteSpace(RQ.hotel) ? null : Convert.ToInt32(RQ.hotel)),
                                    CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds,
                                    Channelcode = Convert.ToString(RQ.channel)

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            //return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 200, agodaError, "application/json", RQ, agodaError, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.DUPLICATION, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);
                            return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 200, agodaError, "application/json", RQ, agodaError, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId); //28052026

                        }
                        if (agodaBookingRS.status == "FAILED")
                        {
                            agodaError.responseStatus = "SUCCESS";
                            agodaError.remark = agodaBookingRS.remark;
                            agodaError.status = "SUCCESS";
                            agodaError.referenceId = agodaBookingRS.referenceId;
                            agodaError.bookingReferenceId = agodaBookingRS.bookingReferenceId;

                            timer.Stop();
                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "Booking",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(404),
                                    Stage = "Book",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = agodaBookingRS.status,
                                    Errorcode = "NOT_SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = agodaError.remark,
                                    srphotelid = Convert.ToInt32(string.IsNullOrWhiteSpace(RQ.hotel) ? null : Convert.ToInt32(RQ.hotel)),
                                    CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds,
                                    Channelcode = Convert.ToString(RQ.channel)

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            //return Request.CreateResponseCompressed_agodabooking(HttpStatusCode.OK, bookingerrorcode, "application/json", RQ, bookingerrorcode, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);
                            return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 200, bookingerrorcode, "application/json", RQ, bookingerrorcode, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);

                        }
                        if (agodaBookingRS.status == "BOOKING_FAILED")
                        {
                            bookingerrorcode.responseStatus = "BOOKING_FAILED";
                            bookingerrorcode.remark = agodaBookingRS.remark;
                            timer.Stop();
                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "Booking",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(404),
                                    Stage = "Book",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = agodaBookingRS.status,
                                    Errorcode = "NOT_SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = bookingerrorcode.remark,
                                    srphotelid = Convert.ToInt32(string.IsNullOrWhiteSpace(RQ.hotel) ? null : Convert.ToInt32(RQ.hotel)),
                                    CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds,
                                    Channelcode = Convert.ToString(RQ.channel)

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 200, bookingerrorcode, "application/json", RQ, bookingerrorcode, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);
                        }
                        if (agodaBookingRS == null || agodaBookingRS.status == null)
                        {
                            //erroragoda = _hotelXML.GetAgodaErrorMsg("BOOKING_FAIL", "", "cannot create booking.");
                            erroragoda = _hotelXML.GetAgodaErrorMsg("BOOKING_FAIL", _keySettings.AgodaVersion, "cannot create booking.");
                            timer.Stop();
                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "Booking",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(404),
                                    Stage = "Book",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = erroragoda.responseStatus,
                                    Errorcode = "NOT_SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = erroragoda.remark,
                                    srphotelid = Convert.ToInt32(string.IsNullOrWhiteSpace(RQ.hotel) ? null : Convert.ToInt32(RQ.hotel)),
                                    CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds,
                                    Channelcode = Convert.ToString(RQ.channel)

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            //return Request.CreateResponseCompressed(HttpStatusCode.OK, erroragoda, "application/json", data, erroragoda, TokenData, "booking", true, timer.Elapsed.ToString(), "", "0");
                            return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 200, erroragoda, "application/json", RQ, erroragoda, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);
                        }
                        else
                        {
                            timer.Stop();
                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "Booking",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(200),
                                    Stage = "Book",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = "SUCCESSFULL",
                                    Errorcode = "SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = "SUCCESSFULL",
                                    srphotelid = Convert.ToInt32(string.IsNullOrWhiteSpace(RQ.hotel) ? null : Convert.ToInt32(RQ.hotel)),
                                    CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds,
                                    Channelcode = Convert.ToString(RQ.channel)

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            //return Request.CreateResponseCompressed(HttpStatusCode.OK, agodaBookingRS, "application/json", data, agodaBookingRS, TokenData, "booking", true, timer.Elapsed.ToString(), "", "0");
                            // return Request.CreateResponseCompressed_agodabooking(HttpStatusCode.OK, agodaBookingRS, "application/json", RQ, agodaBookingRS, TokenData, "booking", true, timer.Elapsed.ToString(), ErrorCode.NoError, "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);
                            return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 200, agodaBookingRS, "application/json", RQ, agodaBookingRS, TokenData, "booking", true, timer.Elapsed.ToString(), "SUCCESSFULL", "0", agodaBookingRS.referenceId, agodaBookingRS.bookingReferenceId);


                        }

                    }

                }
                else
                {
                   
                    erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", "", "Access credential is incorrect");
                    timer.Stop();
                    #region Elastic
                    try
                    {
                        _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                        {
                            Action = "Booking",
                            //Result = lstFiltOption,
                            HttpStatusCode = Convert.ToInt32(401),
                            Stage = "Book",
                            ElapsedTime = timer.ElapsedMilliseconds,
                            Description = erroragoda.responseStatus,
                            Errorcode = "UNAUTHORIZED",
                            source = "APIOut",
                            AgentName = "Agoda",
                            ActualDescription = erroragoda.remark,
                            srphotelid = Convert.ToInt32(string.IsNullOrWhiteSpace(RQ.hotel) ? null : Convert.ToInt32(RQ.hotel)),
                            CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds,
                            Channelcode = Convert.ToString(RQ.channel)

                        });
                    }
                    catch (Exception exx)
                    {
                        await _exception.Error(exx);
                    }
                    #endregion
                    // return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0");
                    //return await _hotelXML.CreateResponseCompressed_agodahotelsearch(HttpContext, 401, erroragoda, "application/json", RQ, erroragoda, TokenData, "Booking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0"); // needtochange
                    return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 401, erroragoda, "application/json", RQ, erroragoda, TokenData, "Booking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0"); // needtochange //28052026

                }
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                erroragoda = _hotelXML.GetAgodaErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                timer.Stop();
                #region Elastic
                try
                {
                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                    {
                        Action = "Booking",
                        //Result = lstFiltOption,
                        HttpStatusCode = Convert.ToInt32(500),
                        Stage = "Book",
                        ElapsedTime = timer.ElapsedMilliseconds,
                        Description = erroragoda.responseStatus,
                        Errorcode = "INTERNAL_ERROR",
                        source = "APIOut",
                        AgentName = "Agoda",
                        ActualDescription = erroragoda.remark,
                        srphotelid = Convert.ToInt32(RQ.hotel),
                        CodeProcessingTimenew = codetimer.Elapsed.TotalMilliseconds

                    });
                }
                catch (Exception exx)
                {
                    await _exception.Error(exx);
                }
                #endregion

                //HotelXML.EnterBookingApiLogs(JsonConvert.SerializeObject(RQ), ex.Message, "");
               // return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 500, erroragoda, "application/json", RQ, erroragoda, TokenData, "booking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                return await _hotelXML.CreateResponseCompressed_agodabooking(HttpContext, 500, erroragoda, "application/json", RQ, erroragoda, TokenData, "booking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.SomeError, "0"); //28052026

            }

            return null;


        }

        [Route("/v1.9/bookingIds")]
        [HttpPost]
        public async Task<IActionResult> bookingIds(AgodaBookingIDsRQ RQ)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            CommonRequest data = new CommonRequest();
            data.AdvancedOptions = new AdvancedOptions();
            try
            {
                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetUserPlatform(IP);
                //HotelXML.EnterBookingApiLogs(JsonConvert.SerializeObject(RQ), "", "");
                data.Request = RQ.ToString();
                //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                AuthData authData = await _authenticationTokenAgent.AgodaAuthentication(TokenData, data);

                if (!Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", _keySettings.AgodaVersion, "Access credential is incorrect");
                    //erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", "", "Access credential is incorrect");
                    //return await _hotelXML.CreateResponseCompressed_agodabookingIds(HttpContext, 401, erroragoda, "application/json", RQ, erroragoda, TokenData, "bookingIds", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                    return await _hotelXML.CreateResponseCompressed_agodabookingIds(HttpContext, 401, erroragoda, "application/json", RQ, erroragoda, TokenData, "bookingIds", EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0"); //2805206

                }
                if (true || Convert.ToBoolean(TokenData.AgentId))
                {
                    AgodaBookingIDsRS agodaBookingIDsRS = new AgodaBookingIDsRS();
                    agodaBookingIDsRS = await _hotelXML.AgodaBookingIDsProcess(RQ, TokenData, timer);

                    return await _hotelXML.CreateResponseCompressed_agodabookingIds(HttpContext, 200, agodaBookingIDsRS, "application/json", RQ, erroragoda, TokenData, "bookingIds", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                }
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                erroragoda = _hotelXML.GetAgodaErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                //HotelXML.EnterBookingApiLogs(JsonConvert.SerializeObject(RQ), ex.Message, "");

                return await _hotelXML.CreateResponseCompressed_agodabookingIds(HttpContext, 500, erroragoda, "application/json", RQ, erroragoda, TokenData, "bookingIds", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
            }
            return null;
        }


        [Route("/v1.9/bookingdetail")]
        [HttpPost]
        public async Task<IActionResult> bookingdetail(AgodaBookingDetailRQ RQ)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            CommonRequest data = new CommonRequest();
            data.AdvancedOptions = new AdvancedOptions();
            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();
            try
            {
                //HotelXML.EnterBookingApiLogs(JsonConvert.SerializeObject(RQ), "", "");
                data.Request = RQ.ToString();
                //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                AuthData authData = await _authenticationTokenAgent.AgodaAuthentication(TokenData, data);

                if (!Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", _keySettings.AgodaVersion, "Access credential is incorrect");
                   // return await _hotelXML.CreateResponseCompressed_AgodaBookingDetail(HttpContext, 401, erroragoda, "application/json", RQ, erroragoda, TokenData, "bookingdetail", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                    return await _hotelXML.CreateResponseCompressed_AgodaBookingDetail(HttpContext, 401, erroragoda, "application/json", RQ, erroragoda, TokenData, "bookingdetail", EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0"); //28052026

                }
                //if (Convert.ToBoolean(TokenData.AgentId))
                //if (true || Convert.ToBoolean(TokenData.AgentId))
                if (true || Convert.ToBoolean(authData.tokenData.AgentId)) // 24052026

                {
                    AgodaBookingDetailRSF.AgodaBookingDetailRS agodaBookingDetailRS = new AgodaBookingDetailRSF.AgodaBookingDetailRS();
                    //agodaBookingDetailRS = await _hotelXML.AgodaBookingDetailProcess(RQ, TokenData, timer);
                    agodaBookingDetailRS = await _hotelXML.AgodaBookingDetailProcess(RQ, authData.tokenData, timer);

                    //return _hotelXML.CreateResponseCompressed_AgodaBookingDetail(HttpStatusCode.OK, agodaBookingDetailRS, "application/json", RQ, agodaBookingDetailRS, TokenData, "bookingIds", true, timer.Elapsed.ToString(), "SUCCESSFULL", "0", agodaBookingDetailRS);
                    return await _hotelXML.CreateResponseCompressed_AgodaBookingDetail(HttpContext, 200, agodaBookingDetailRS, "application/json", RQ, agodaBookingDetailRS, TokenData, "bookingdetail", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");

                }
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                await _exception.Error(ex);
                erroragoda = _hotelXML.GetAgodaErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                //HotelXML.EnterBookingApiLogs(JsonConvert.SerializeObject(RQ), ex.Message, "");

                return await _hotelXML.CreateResponseCompressed_AgodaBookingDetail(HttpContext, 500, erroragoda, "application/json", RQ, erroragoda, TokenData, "bookingdetail", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
            }
            return null;
        }



        [HttpPost]
        [Route("/v1.9/cancel")]
        public async Task<IActionResult> cancel(AgodaCancelRQ cancelRQ)//object json, TokenData Tokendata)

        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();
            CommonRequest data = new CommonRequest();
            data.AdvancedOptions = new AdvancedOptions();
            string IP = _platformService.GetIPAddress();
            string userAgent = _platformService.GetUserPlatform(IP);
            string MainURL = _platformService.GetHostName();

            try
            {
                cancelRQ.token = _keySettings.agoda_apikey;
                data.Request = cancelRQ.ToString();

               // AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                AuthData authData = await _authenticationTokenAgent.AgodaAuthentication(TokenData, data);
                //if (Convert.ToBoolean(authData.tokenData.AgentId))

                //{
                //    //erroragoda = HotelXML.GetAgodaErrorMsg("UNAUTHORIZED", version.Version, "Access credential is incorrect");
                //    erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", "", "Access credential is incorrect");

                //    //erroragoda = HotelXML.GetAgodaErrorMsg(AgodaErrorType.unAuthorized, AgodaErrorDes.unAuthorized);
                //    //return Request.CreateResponseCompressed_agodacancel(HttpStatusCode.Unauthorized, erroragoda, "application/json", cancelRQ, erroragoda, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0");

                //}
                if (Convert.ToBoolean(authData.tokenData.AgentId) && cancelRQ.referenceId != null)

                {
                    cancelRQ.referenceId = cancelRQ.referenceId.ToLower().Replace("agoda_", "");
                    data.AdvancedOptions.Currency = TokenData.CreditLimitCurrencyName;// use to get data in credit currecny only
                    CancelBookingRequest payload = new CancelBookingRequest();
                    CancelRQ RQ = new CancelRQ();
                    RQ.BookingDetailId = 0;
                    RQ.BookingId = 0;
                    RQ.CancelAll = 1;
                    RQ.InternalReference = cancelRQ.referenceId;
                    RQ.Reason = "Cancellation by agoda";
                    RQ.ReferenceNo = "";
                    payload.CancelRQ = RQ;


                    // CancelBookingResponse_agoda lstFiltOption = await _hotelXML.CancelBooking(payload, TokenData, data.AdvancedOptions, userAgent, IP, MainURL);
                    CancelBookingResponse_agoda lstFiltOption = await _hotelXML.CancelBooking_agoda(payload, TokenData, data.AdvancedOptions, userAgent, IP, MainURL);

                    AgodaCancelRS cancellation = new AgodaCancelRS();
                    cancellation.referenceId = cancelRQ.referenceId;
                    cancellation.cancellationFees = 0;

                    if (lstFiltOption != null)
                    {
                        if (lstFiltOption.Error.Count > 0)
                        {
                            timer.Stop();
                            erroragoda = _hotelXML.GetAgodaErrorMsg("CANCEL_FAIL", "", "Cancel Order Failed");

                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "cancel",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(404),
                                    Stage = "Cancel",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = erroragoda.responseStatus,
                                    Errorcode = "NOT_SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = erroragoda.remark,
                                    //CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion

                            return await _hotelXML.CreateResponseCompressed_agodacancel(HttpContext, 401, erroragoda, "application/json", cancelRQ, erroragoda, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", HttpStatusCode.NotFound); // needtochange


                        }
                        else if (lstFiltOption.CancelRS.HotelOption.HotelRooms.Count == 0)
                        {
                            timer.Stop();
                            // lstFiltOption.Error.Add(HotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NoPermission_Cancel));
                            erroragoda = _hotelXML.GetAgodaErrorMsg("CANCEL_FAIL", "", "Cancel Order Failed");

                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "cancel",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(404),
                                    Stage = "Cancel",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = erroragoda.responseStatus,
                                    Errorcode = "NOT_SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = erroragoda.remark,
                                    //CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion


                            return await _hotelXML.CreateResponseCompressed_agodacancel(HttpContext, 200, erroragoda, "application/json", cancelRQ, erroragoda, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.SomeError, "0", HttpStatusCode.NotFound); // needtochange


                        }
                        else
                        {
                            lstFiltOption.CancelRS.Currency = TokenData.CreditLimitCurrencyName;

                            double charge = 0;
                            for (int i = 0; i < lstFiltOption.CancelRS.HotelOption.HotelRooms.Count; i++)
                            {
                                //if(lstFiltOption.CancelRS.HotelOption.HotelRooms[i].CancelStatus==1)
                                charge += lstFiltOption.CancelRS.HotelOption.HotelRooms[i].AgentCharge;
                            }
                            cancellation.cancellationFees = charge;

                            timer.Stop();

                            #region Elastic
                            try
                            {
                                _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                                {
                                    Action = "cancel",
                                    //Result = lstFiltOption,
                                    HttpStatusCode = Convert.ToInt32(200),
                                    Stage = "Cancel",
                                    ElapsedTime = timer.ElapsedMilliseconds,
                                    Description = "SUCCESSFULL",
                                    Errorcode = "SUCCESSFULL",
                                    source = "APIOut",
                                    AgentName = "Agoda",
                                    ActualDescription = "SUCCESSFULL",
                                    //CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds

                                });
                            }
                            catch (Exception exx)
                            {
                                await _exception.Error(exx);
                            }
                            #endregion
                            // return Request.CreateResponseCompressed_agodacancel(HttpStatusCode.OK, cancellation, "application/json", cancelRQ, lstFiltOption, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0");
                            return await _hotelXML.CreateResponseCompressed_agodacancel(HttpContext, 200, cancellation, "application/json", cancelRQ, lstFiltOption, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0", HttpStatusCode.OK); // needtochange


                        }
                    }
                    else
                    {
                        timer.Stop();
                        //Common.Error(ex);
                        erroragoda = _hotelXML.GetAgodaErrorMsg("SUPPLIER_SERVER_ERROR", "", "Supplier service unavailable/server down. Need to escalate to supplier.");
                        //lstFiltOption.Error.Add(error);

                        #region Elastic
                        try
                        {
                            _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                            {
                                Action = "cancel",
                                //Result = lstFiltOption,
                                HttpStatusCode = Convert.ToInt32(500),
                                Stage = "Cancel",
                                ElapsedTime = timer.ElapsedMilliseconds,
                                Description = erroragoda.responseStatus,
                                Errorcode = "INTERNAL_ERROR",
                                source = "APIOut",
                                AgentName = "Agoda",
                                ActualDescription = erroragoda.remark,
                                //CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds

                            });
                        }
                        catch (Exception exx)
                        {
                            await _exception.Error(exx);
                        }
                        #endregion
                        return await _hotelXML.CreateResponseCompressed_agodacancel(HttpContext, 200, erroragoda, "application/json", cancelRQ, erroragoda, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", HttpStatusCode.InternalServerError); // needtochange

                    }
                }
                else
                {
                    
                    erroragoda = _hotelXML.GetAgodaErrorMsg("UNAUTHORIZED", "", "Access credential is incorrect");
                    timer.Stop();
                    #region Elastic
                    try
                    {
                        _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                        {
                            Action = "cancel",
                            //Result = lstFiltOption,
                            HttpStatusCode = Convert.ToInt32(401),
                            Stage = "Cancel",
                            ElapsedTime = timer.ElapsedMilliseconds,
                            Description = erroragoda.responseStatus,
                            Errorcode = "UNAUTHORIZED",
                            source = "APIOut",
                            AgentName = "Agoda",
                            ActualDescription = erroragoda.remark,
                            //CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds

                        });
                    }
                    catch (Exception exx)
                    {
                        await _exception.Error(exx);
                    }
                    #endregion
                    // return Request.CreateResponseCompressed_agodahotelsearch(HttpStatusCode.OK, erroragoda, "application/json", commonRQ, erroragoda, tokenData, requestName, EnterLogs, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "0");
                    return await _hotelXML.CreateResponseCompressed_agodacancel(HttpContext, 401, erroragoda, "application/json", cancelRQ, erroragoda, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.NoError, "0", HttpStatusCode.OK); // needtochange

                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);  //26052026
                // error = HotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                CancelBookingResponse rs = new CancelBookingResponse();
                rs.Error.Add(error);
                erroragoda = _hotelXML.GetAgodaErrorMsg("SUPPLIER_SERVER_ERROR", "", "Supplier service unavailable/server down. Need to escalate to supplier.");
                #region Elastic
                try
                {
                    _analytic.PushHotelDetailsClientDataForAnalyticsAsyc_Agoda(new AnalyticsData
                    {
                        Action = "cancel",
                        //Result = lstFiltOption,
                        HttpStatusCode = Convert.ToInt32(500),
                        Stage = "Cancel",
                        ElapsedTime = timer.ElapsedMilliseconds,
                        Description = erroragoda.responseStatus,
                        Errorcode = "INTERNAL_ERROR",
                        source = "APIOut",
                        AgentName = "Agoda",
                        ActualDescription = erroragoda.remark,
                        //CodeProcessingTimenew = timerforagodaconvert.Elapsed.TotalMilliseconds

                    });
                }
                catch (Exception exx)
                {
                    await _exception.Error(exx);
                }
                #endregion
                return await _hotelXML.CreateResponseCompressed_agodacancel(HttpContext, 200, erroragoda, "application/json", cancelRQ, erroragoda, TokenData, "CancelBooking", EnterLogs, timer.Elapsed.ToString(), ErrorCode.InternalError, "0", HttpStatusCode.InternalServerError);

            }
        }


        #endregion
        [AllowAnonymous]
        [Route("GetRoomMappingHotelReport")]
        [HttpPost]
        public async Task<IActionResult> GetRoomMappingHotel(CommonRequest data)

        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                ApiRequestRoomMapping payload = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRequestRoomMapping>(data.Request.ToString());
                    List<RoomMappingReport> lstFiltOption = await _hotelXML.GetRoomMappingHotelReport(payload);
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
            
                
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }

        [Route("GetWhiteListIP")]

        [HttpGet]
        public async Task<IActionResult> GetWhiteListIP()

        {

            Stopwatch timer = new Stopwatch();

            timer.Start();

            TokenData TokenData = new TokenData();

            CommonRequest data = new CommonRequest();

            data.IsSearch = 1;

            try

            {

                //string IP = HttpContext.Items["ClientIp"]?.ToString();

                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();

                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);

                if (Convert.ToBoolean(authData.tokenData.AgentId))

                {

                    List<IpList> lstFiltOption = await _hotelXML.GetAgentIps(Convert.ToInt32(authData.tokenData.AgentId), Convert.ToString(authData.tokenData.TanentId));

                    if (lstFiltOption != null)

                    {

                        timer.Stop();

                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "GetWhiteListIP", true, timer.Elapsed.ToString(), ErrorCode.NoError, "", IP, "", "", "", 0, 0, 0, "", null);

                    }

                    else

                    {

                        timer.Stop();

                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);

                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetWhiteListIP", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", IP, "", "", "", 0, 0, 0, "", null);

                    }

                }

                else

                {

                    timer.Stop();

                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetWhiteListIP", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);

                }

            }

            catch (Exception ex)

            {

                timer.Stop();

                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);

                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetWhiteListIP", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }
        [AllowAnonymous]
        [Route("GetSupplierperformanceReport")]
        [HttpPost]
        public async Task<IActionResult> GetSupplierperformanceReport(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                string IP = _platformService.GetIPAddress();
                string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());


                //AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                //if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    SprRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<SprRequest>(data.Request.ToString());
                    List<SupplierperformanceReport> lstFiltOption = await _hotelXML.GetSupplierperformanceReport(payload);
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        return Ok(lstFiltOption);
                    }
                    else
                    {
                        timer.Stop();
                        return NotFound(StatusCodes.Status500InternalServerError);
                    }
                }
                //else
                //{
                //    timer.Stop();
                //    return NotFound(StatusCodes.Status401Unauthorized);
                //}
            }
            catch (Exception ex)
            {
                timer.Stop();
                await _exception.Error(ex);
                return NotFound(StatusCodes.Status500InternalServerError);

            }

        }

        [Route("GetHotelGuestReview")]
        [HttpPost]
        public async Task<IActionResult> GetHotelGuestReview(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                //bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                //if (Convert.ToBoolean(authData.tokenData.AgentId))
                if (true)
                {
                    HotelReviewRequest payload = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelReviewRequest>(data.Request.ToString());

                    // List<StaticData> lstFiltOption = await _hotelXML.GetCitiesWithHotel(payload.CityName);
                    List<GuestReviewModel> lstFiltOption = await _hotelXML.GetHotelGuestReviewAsync(payload.HotelId, data.AdvancedOptions);
                    if (lstFiltOption != null)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "GetCitiesWithHotel", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.HotelId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetCitiesWithHotel", true, timer.Elapsed.ToString(), ErrorCode.InternalError, payload.HotelId.ToString(), IP, "", "", "", 0, 0, 0, "", null);
                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetCitiesWithHotel", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetStaticDataByCity", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

        [Route("GetHotelRoomData")]
        [HttpPost]
        public async Task<IActionResult> GetHotelRoomData(CommonRequest data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            TokenData TokenData = new TokenData();

            try
            {
                //string IP = HttpContext.Items["ClientIp"]?.ToString();
                //var userAgent = HttpContext.Items["User-Agent"]?.ToString();

                string IP = _platformService.GetIPAddress();
                //string userAgent = _platformService.GetBrowserInfo(_platformService.GetRequest());

                string userAgent = _platformService.GetUserPlatform(IP);
                string MainURL = _platformService.GetHostName();

                //bool IsRefereshCache = data.AdvancedOptions.CustomerIpAddress == "777" ? true : false;

                AuthData authData = await _authenticationTokenAgent.Authenticate(TokenData, data, IP, userAgent);
                if (Convert.ToBoolean(authData.tokenData.AgentId))
                {
                    GetHotelroomdata payload = Newtonsoft.Json.JsonConvert.DeserializeObject<GetHotelroomdata>(data.Request.ToString());

                    // List<StaticData> lstFiltOption = await _hotelXML.GetCitiesWithHotel(payload.CityName);
                    List<HotelRoomResult> lstFiltOption = await _hotelXML.GetHotelRoomData(payload.HotelId, data.AdvancedOptions, Convert.ToString(authData.tokenData.TanentId), Convert.ToString(authData.tokenData.AgentId), userAgent);
                    if (lstFiltOption != null && lstFiltOption.Count > 0)
                    {
                        timer.Stop();
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.OK, lstFiltOption, "application/json", data, lstFiltOption, TokenData, "GetHotelRoomData", true, timer.Elapsed.ToString(), ErrorCode.NoError, payload.HotelId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }

                    else
                    {
                        timer.Stop();
                        error = _hotelXML.GetErrorMsg(ErrorType.GeneralError, ErrorCode.NorecordError, ErrorDescription.NorecordError);
                        return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.NotFound, lstFiltOption, "application/json", data, lstFiltOption, authData.tokenData, "availabilitywithCancellation", true, timer.Elapsed.ToString(), ErrorCode.NorecordError, payload.HotelId.ToString(), IP, "", "", "", 0, 0, 0, "", null);

                    }
                }
                else
                {
                    timer.Stop();
                    return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.Unauthorized, 401, "application/json", data, "", authData.tokenData, "GetHotelRoomData", true, timer.Elapsed.ToString(), ErrorCode.AutorizationError, "", IP, "", "", "", 0, 0, 0, "", null);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                error = _hotelXML.GetErrorMsg(ErrorType.InternalError, ErrorCode.InternalError, ErrorDescription.InternalError);
                return await _hotelXML.CreateResponseCompressedAsync(HttpContext, HttpStatusCode.InternalServerError, error, "application/json", data, "", TokenData, "GetHotelRoomData", true, timer.Elapsed.ToString(), ErrorCode.InternalError, "", "", "", "", "", 0, 0, 0, "", null);

            }

        }

    }
}


