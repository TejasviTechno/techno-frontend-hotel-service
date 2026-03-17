using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QueueManager.Contracts.Models.Hotel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UserContextMiddleWare.Services;
using XConnect.Analytics.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using static XConnect.Analytics.PrepareClientData;


namespace XConnect.Analytics
{
    public class PrepareClientData : IAnalytics
    {
        private readonly IPushAnalytics _pushAnalytics;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly ILogger<PrepareClientData> _logger;
        private readonly IConfiguration _configuration;


        public PrepareClientData(IPushAnalytics pushAnalytics, IUserContextAccessor userContextAccessor, ILogger<PrepareClientData> logger, IConfiguration configuration)
        {
            _pushAnalytics = pushAnalytics;
            _userContextAccessor = userContextAccessor;
            _logger = logger;
            _configuration = configuration;
        }

     
        public void PushHotelDetailsClientDataForAnalyticsAsyc(AnalyticsData analyticsData)
        {
            try
            {
                var isDebugMode = _configuration.GetValue<bool>("EnvironmentType:isDebug");
                if (isDebugMode)
                {
                    _logger.LogError("Entry at PushHotelDetailsClientDataForAnalyticsAsyc");
                }


                var resultData = analyticsData.Result as AvailHotelList_Comp;  // response 

                var mainSearchPayload = analyticsData.Payload as HotelSearchParameter; // request

               
                var clientsLocalTimeZone = _configuration.GetValue<string>("EnvironmentType:clientsLocalTimeZone") ?? "Arabian Standard Time";

                var locationItem = resultData?.AvailabilityRS?.HotelResult?.FirstOrDefault();

                

                var locationInfo = new AnalyticsLocationInfo
                {

                    City = mainSearchPayload?.CityName ?? "anonymous",
                    Country = mainSearchPayload?.CountryCode ?? "anonymous",
                    Lat = 0,
                    Long = 0,
                    IsoCountry = mainSearchPayload?.CountryCode ?? "anonymous",
                    IsoNationality = mainSearchPayload?.Nationality ?? "anonymous",
                    LocationType = mainSearchPayload?.CountryCode ?? "anonymous",
                    Nationality = mainSearchPayload?.Nationality ?? "anonymous",

                };
               

                var productInfo = new AnalyticsProductInfo
                {
                    Name = analyticsData?.Search_Key ?? "anonymous",

                    //Name = resultData?.AvailabilityRS?.SearchKey ?? "anonymous",//productDetail?.RestaurantNameDefault ?? "anonymous",
                    City = mainSearchPayload?.CityID.ToString() ?? "anonymous",
                    Country = mainSearchPayload?.CountryID.ToString() ?? "anonymous",
                };
                
                //var productInfo = new AnalyticsProductInfo();

                var fareDetails = new AnalyticsFareInformation
                {
                    TotalProducts = Convert.ToInt32(resultData?.AvailabilityRS?.Count ?? 0),
                    Volume = 0
                };

                int NoofAdults = 0;
                int NoofChild = 0;
                for (int i = 0; i < mainSearchPayload?.Rooms?.Count; i++)
                {
                    NoofAdults = NoofAdults + mainSearchPayload.Rooms[i]?.NoofAdults ?? 0;
                    NoofChild = NoofChild + mainSearchPayload.Rooms[i]?.NoofChild ?? 0;
                }

                var paxDetails = new AnalyticsPaxInformation
                {
                    Adults = NoofAdults,
                    Children = NoofChild,
                    Nights = mainSearchPayload?.NoofNights ?? 0,
                };


                var service = new ClientSearchDataService(_userContextAccessor, mainSearchPayload, locationInfo, analyticsData, productInfo, fareDetails, paxDetails);


                var browserInfo = service.GetBrowserInfo();
                var apiInfo = service.GetApiInfo();
                var userInfo = service.GetUserInfo();
                var paxInfo = service.GetPaxAndDurationInfo(NoofAdults, NoofChild);
                var requestInfo = service.GetRequestInfo(isDebugMode);
                var locationDetails = service.GetLocationDetails();

                var clientDetails = service.GetClientDetails();
                var search = service.GetSearchPayload();
                var productDetails = service.GetProductDetails();
                /*var productDetails = "";*/// resultData?.detail != null ? service.GetProductDetails() : null;

                var fareInfo = service.GetFareInfo();
                var SearchAvailTrace = service.GetSearchAvailTrace();

                if (isDebugMode)
                {
                    _logger.LogError("Entry2  at PushHotelDetailsClientDataForAnalyticsAsyc");
                }


                var clientData = new ClientData
                {
                    ServiceName = QueueManager.Contracts.Enums.Constants.ElasticsearchIndexConstants.Client_HotelService,
                    clientTimeZone = string.IsNullOrEmpty(_userContextAccessor.ClientStats.clientsLocalTimeZone) ? clientsLocalTimeZone : _userContextAccessor.ClientStats.clientsLocalTimeZone,
                    Description = analyticsData.Description,
                    ApiInfo = apiInfo,
                    UserInfo = userInfo,
                    PaxAndDuration = paxInfo,
                    RqRsDetails = requestInfo,
                    Location = locationDetails,
                    Products = productDetails != null && productDetails.Any() ? productDetails : [new SearchProductDetails()],
                    TransferLocationDetails = null, //transferDetails,
                    FareInfo = fareInfo,
                    client = clientDetails,
                    BrowserAgent = browserInfo,
                    SiteType = _userContextAccessor.SiteType,
                    SearchAvailTrace= SearchAvailTrace
                };


                _pushAnalytics.PushClientDataForAnalyticsAsyc(clientData);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error at PushHotelDetailsClientDataForAnalyticsAsyc");

            }
        }

      

    }

    public class PrepareSupplierData : IAnalyticsSupplier
    {
        private readonly IPushAnalyticsSupplier _pushSupplierAnalytics;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly ILogger<PrepareSupplierData> _logger;
        private readonly IConfiguration _configuration;


        public PrepareSupplierData(IPushAnalyticsSupplier pushSupplierAnalytics, IUserContextAccessor userContextAccessor, ILogger<PrepareSupplierData> logger, IConfiguration configuration)
        {
            _pushSupplierAnalytics = pushSupplierAnalytics;
            _userContextAccessor = userContextAccessor;
            _logger = logger;
            _configuration = configuration;
        }

        public void PushHotelSearchSupplierDataForAnalyticsAsyc(AnalyticsData analyticsData)
        {
            try
            {


                var supplierDataList = new List<SupplierData>();

                var allTasks = new List<HotelX>();

                if (analyticsData?.Result is ConcurrentBag<HotelX> bundles)
                //if (analyticsData?.Result is IEnumerable<HotelX> bundles)
                {
                    allTasks.AddRange(bundles);
                }

                var mainSearchPayload = analyticsData.Payload as HotelSearchParameter; // request

               

                var isDebugMode = _configuration.GetValue<bool>("EnvironmentType:isDebug");
                var clientsLocalTimeZone = _configuration.GetValue<string>("EnvironmentType:clientsLocalTimeZone") ?? "Arabian Standard Time";

                if (allTasks != null)
                {
                    for (int i = 0; i < allTasks.Count; i++)
                    {
                        //under work
                        var analyticsDataSupplier = new AnalyticsData
                        {
                            Action = analyticsData.Action,
                            Version = analyticsData.Version,
                            HttpStatusCode = allTasks[i].ResponseStatus,// allTasks[i].SupplierResponse.HttpStatusCode,
                            Stage = analyticsData.Stage,
                            ElapsedTime = (long)TimeSpan.Parse(allTasks[i].RSTimeout).TotalMilliseconds,
                            Description = allTasks[i].ResponseType,

                        };
                        


                        var locationInfo = new AnalyticsLocationInfo
                        {
                            City = mainSearchPayload?.CityName ?? "anonymous",
                            Country = mainSearchPayload?.CountryCode ?? "anonymous",
                            Lat = 0,
                            Long = 0,
                            IsoCountry = mainSearchPayload?.CountryCode ?? "anonymous",
                            IsoNationality = mainSearchPayload?.Nationality ?? "anonymous",
                            LocationType = mainSearchPayload?.CountryCode ?? "anonymous",
                            Nationality = mainSearchPayload?.Nationality ?? "anonymous",

                        };

                        var productInfo = new AnalyticsProductInfo
                        {

                            //Id = mainSearchPayload?.Filters?.HotelIDs ??,// productDetail?.RestaurantId ?? 0,
                            Name = analyticsData?.Search_Key ?? "anonymous",
                            City = mainSearchPayload?.CityID.ToString() ?? "anonymous",
                            Country = mainSearchPayload?.CountryID.ToString() ?? "anonymous",

                        };
                        //var productInfo = new AnalyticsProductInfo();
                        var fareDetails = new AnalyticsFareInformation
                        {

                            TotalProducts = allTasks.Count,
                            Volume = 0
                        };
                        int NoofAdults = 0;
                        int NoofChild = 0;

                        for (int ii = 0; ii < mainSearchPayload?.Rooms?.Count; ii++)
                        {
                            NoofAdults = NoofAdults + mainSearchPayload.Rooms[ii]?.NoofAdults ?? 0;
                            NoofChild = NoofChild + mainSearchPayload.Rooms[ii]?.NoofChild ?? 0;
                        }


                        var paxDetails = new AnalyticsPaxInformation
                        {
                            Adults = NoofAdults,
                            Children = NoofChild,
                            Nights = mainSearchPayload?.NoofNights ?? 0,
                        };


                        var service = new ClientSearchDataService(_userContextAccessor, mainSearchPayload, locationInfo, analyticsDataSupplier, productInfo, fareDetails, paxDetails);


                        var browserInfo = service.GetBrowserInfo();
                        var apiInfo = service.GetApiInfo();
                        var userInfo = service.GetUserInfo();
                        var paxInfo = service.GetPaxAndDurationInfo(NoofAdults, NoofChild);
                        var requestInfo = service.GetRequestInfo(isDebugMode);
                        var locationDetails = service.GetLocationDetails();

                        var clientDetails = service.GetClientDetails();
                        var search = service.GetSearchPayload();
                        var productDetails = service.GetProductDetails();
                        var fareInfo = service.GetFareInfo();



                        var supplierData = new SupplierData
                        {
                            TechnologyPartner = allTasks[i].TechName,
                            SupplierName = allTasks[i].Supplier,
                            ServiceName = QueueManager.Contracts.Enums.Constants.ElasticsearchIndexConstants.Supplier_HotelService,
                            clientTimeZone = string.IsNullOrEmpty(_userContextAccessor.ClientStats.clientsLocalTimeZone) ? clientsLocalTimeZone : _userContextAccessor.ClientStats.clientsLocalTimeZone,
                            Description = analyticsData.Description,
                            ApiInfo = apiInfo,
                            UserInfo = userInfo,
                            PaxAndDuration = paxInfo,
                            RqRsDetails = requestInfo,
                            Location = locationDetails,
                            Products = productDetails != null && productDetails.Any() ? productDetails : [new SearchProductDetails()],
                            TransferLocationDetails = null, 
                            FareInfo = fareInfo,
                            client = clientDetails,
                            BrowserAgent = browserInfo,
                            SiteType = _userContextAccessor.SiteType
                        };
                        supplierDataList.Add(supplierData);
                    }
                }

                _pushSupplierAnalytics.PushSupplierDataForAnalyticsAsyc(supplierDataList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error at PushHotelSearchSupplierDataForAnalyticsAsyc");
            }
        }

        

    }
    public class ClientSearchDataService
    {
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly AnalyticsData _analyticsData;
        private readonly HotelSearchParameter _searchPayload;
        private readonly AnalyticsLocationInfo _locationInfo;
        private readonly AnalyticsProductInfo _productInfo;
        private readonly AnalyticsFareInformation _fareInfo;
        private readonly AnalyticsPaxInformation _paxInfo;

        public ClientSearchDataService(IUserContextAccessor userContextAccessor, HotelSearchParameter searchPayload, AnalyticsLocationInfo locationInfo, AnalyticsData analyticsData, AnalyticsProductInfo productInfo, AnalyticsFareInformation fareInfo, AnalyticsPaxInformation paxInfo)
        {
            _userContextAccessor = userContextAccessor;
            _searchPayload = searchPayload;
            _locationInfo = locationInfo;
            _analyticsData = analyticsData;
            _productInfo = productInfo;
            _fareInfo = fareInfo;
            _paxInfo = paxInfo;
        }

        public BrowserInfo GetBrowserInfo()
        {
            return new BrowserInfo
            {
                BwsrName = _userContextAccessor.BrowserStats.BwsrName,
                OsDetails = _userContextAccessor.BrowserStats.OsDetails,
                DeviceType = _userContextAccessor.BrowserStats.DeviceType,
                UserAgent = _userContextAccessor.BrowserStats.UserAgent,
                Lang = _userContextAccessor.BrowserStats.Lang,
            };
        }

        public ApiInformation GetApiInfo()
        {
            return new ApiInformation
            {
                ApiName = _analyticsData.Action,
                Version = _analyticsData.Version,
                StatusCode = _analyticsData.HttpStatusCode,
                Stage = _analyticsData.Stage
            };
        }

        public UserInformation GetUserInfo()
        {
            return new UserInformation
            {
                AgentName = _userContextAccessor.DecodedJwt?.UserName ?? "anonymous",
                AgentType = _userContextAccessor.DecodedJwt?.UserType ?? "Agent",
                AgentId = _userContextAccessor.DecodedJwt?.AgentId ?? 0
            };
        }

        public PaxAndDurationInformation GetPaxAndDurationInfo(int NoofAdults, int NoofChild)
        {
            return new PaxAndDurationInformation
            {
                Adults = _paxInfo.Adults,
                Children = _paxInfo.Children,
                Nights = _paxInfo.Nights,
            };
        }

        public RequestInformationAndTiming GetRequestInfo(bool isDebugMode)
        {

            return new RequestInformationAndTiming
            {
                Request = isDebugMode ? JsonSerializer.Serialize(_analyticsData.Payload) : string.Empty,  // request 
                Response = isDebugMode ? JsonSerializer.Serialize(_analyticsData.Result) : string.Empty,  //response
                CoRelationId = _userContextAccessor.CorelationId,
                CheckIn = (_searchPayload == null || string.IsNullOrEmpty(_searchPayload.CheckInDate)) ? DateTime.MinValue : DateTime.ParseExact(_searchPayload.CheckInDate, new[] { "dd/MM/yyyy", "MM-dd-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None),
                CheckOut = (_searchPayload == null || string.IsNullOrEmpty(_searchPayload.CheckOutDate)) ? DateTime.MinValue : DateTime.ParseExact(_searchPayload.CheckOutDate, new[] { "dd/MM/yyyy", "MM-dd-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None),

                

                ElapsedTime = _analyticsData.ElapsedTime.ToString(),
                ElapsedSeconds = _analyticsData.ElapsedTime,
                TraceId = _userContextAccessor.TraceId,
                SearchKey = _userContextAccessor.CorelationId,
                TenantId = Convert.ToString(_userContextAccessor?.DecodedJwt?.TenantId) ?? "",

            };
        }

        public LocationDetails GetLocationDetails()
        {
            return new LocationDetails
            {
                City = _locationInfo.City,
                Country = _locationInfo.Country,
                Lat = _locationInfo.Lat,
                Long = _locationInfo.Long,
                IsoCountry = _locationInfo.IsoCountry,
                IsoNationality = _locationInfo.IsoNationality,
                ClientIp = _userContextAccessor.ClientStats.ClientIp,
                LocationType = _locationInfo.LocationType,
                Nationality = _locationInfo.Nationality,
                ClientCIso = _userContextAccessor.BrowserStats.ClientCIso,
                ClientCity = _userContextAccessor.BrowserStats.ClientCity,
                ClientCountry = _userContextAccessor.BrowserStats.ClientCountry,


            };
        }


        public ClientDetails GetClientDetails()
        {
            return new ClientDetails
            {
                ClientName = _userContextAccessor.ClientStats.ClientName,
            };
        }

        public SearchPayload GetSearchPayload()
        {
            return new SearchPayload
            {
                CheckInDate = (_searchPayload == null || string.IsNullOrEmpty(_searchPayload.CheckInDate)) ? DateTime.MinValue : DateTime.ParseExact(_searchPayload.CheckInDate, new[] { "dd/MM/yyyy", "MM-dd-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None),
                CheckOutDate = (_searchPayload == null || string.IsNullOrEmpty(_searchPayload.CheckOutDate)) ? DateTime.MinValue : DateTime.ParseExact(_searchPayload.CheckOutDate, new[] { "dd/MM/yyyy", "MM-dd-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None),

                
            };
        }

        public List<SearchProductDetails> GetProductDetails()
        {
            return new List<SearchProductDetails>
        {
            new SearchProductDetails
            {
                Id = _productInfo.Id,
                Name =_productInfo.Name,
                City =_productInfo.City,
                Country =_productInfo.Country,


            }
        };
        }



        public FareInformation GetFareInfo()
        {
            return new FareInformation
            {
                Volume = _fareInfo.Volume,
                TotalProducts = _fareInfo.TotalProducts
            };
        }

        public SearchAvailTraces GetSearchAvailTrace()
        {
            
            var allHotelIds = _searchPayload?.Filters?.HotelIDs?.Split(',', StringSplitOptions.RemoveEmptyEntries)?? Array.Empty<string>();
            return new SearchAvailTraces
            {
                SuccessIds = _analyticsData.Errorcode == "SUCCESSFULL" ? allHotelIds : new string[] { "anonymous" },
                FailureIds = _analyticsData.Errorcode != "SUCCESSFULL"? allHotelIds: new string[] { "anonymous" }
                //FailureIds = _analyticsData.Errorcode != "SUCCESSFULL"? allHotelIds: Array.Empty<string>()

            };
        }

    }

    public class AnalyticsLocationInfo
    {
        public string City { get; set; } = "anonymous";
        public string Country { get; set; } = "anonymous";
        public double Lat { get; set; } = 0;
        public double Long { get; set; } = 0;
        public string IsoCountry { get; set; } = "anonymous";
        public string IsoNationality { get; set; } = "anonymous";
        public string LocationType { get; set; } = "anonymous";
        public string Nationality { get; set; } = "anonymous";

    }
    public class AnalyticsProductInfo
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = "anonymous";
        public string SubProduct { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal Sell { get; set; } = 0;
        public string City { get; set; } = "anonymous";
        public string Country { get; set; } = "anonymous";
        public double Lat { get; set; } = 0;
        public double Long { get; set; } = 0;
        public string LocationType { get; set; } = string.Empty;
        public bool IsSuccesful { get; set; } = true;
        public decimal Markup { get; set; } = 0;
        public string MarkupType { get; set; } = string.Empty;
       
    }
    public class AnalyticsFareInformation
    {
        public decimal Volume { get; set; }

        public int TotalProducts { get; set; }
    }
    public class AnalyticsPaxInformation
    {
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Nights { get; set; }
    }

}
