using Xconnect.API.Middleware;
using XConnect.Auth.Interface;
using Xconnect.Auth;
using XConnect.BAL.Interface;
using Xconnect.BLL;
using XConnect.CommonUtilities;
using XConnect.DAL.Interface;
using XConnect.DAL;
using XConnect.ErrorLogHandler.Interface;
using XConnect.ErrorLogHandler;
using XConnect.Markup.Interface;
using XConnect.Markup;
using XConnect.Models;
using XConnect.CommonUtilities.Interface;
using Microsoft.AspNetCore.Mvc;
using XConnect.API.Extensions;
using UserContextMiddleWare.Extensions;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Events;
using Serilog.Formatting.Compact;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;


var builder = WebApplication.CreateBuilder(args);

/* ---------------- AUTOFAC ---------------- */
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    // Register your Autofac modules here
    // container.RegisterModule(new YourModule());
});

//Added on 9-FEB-26
var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
bool tracingEnabled = otelConfig.GetValue<bool>("Enabled");
if (tracingEnabled)
{
    string otlpEndpoint = otelConfig.GetValue<string>("OtlpEndpoint");
    string serviceName = otelConfig.GetValue<string>("ServiceName");

    builder.Services.AddOpenTelemetry()
        .WithTracing(tpb =>
        {
            tpb.SetResourceBuilder(
                    ResourceBuilder.CreateDefault().AddService(serviceName))

                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()

                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForStoredProcedure = true;
                })

                .AddSqlClientInstrumentation(options =>
                {

                    options.RecordException = true;
                })

                  .AddOtlpExporter(opt =>
                  {
                      opt.Endpoint = new Uri(otlpEndpoint);
                      opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                  });
        });
}

/* ---------------- SERILOG ---------------- */
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var environment = context.HostingEnvironment;
    var applicationName = context.HostingEnvironment.ApplicationName;

    loggerConfiguration
#if DEBUG
        .MinimumLevel.Debug()
#else
        .MinimumLevel.Information()
#endif
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        //Added below on 9-FEB-26
.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
.MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Warning)
.MinimumLevel.Override("System", LogEventLevel.Warning)
.MinimumLevel.Override("Volo", LogEventLevel.Warning)
.MinimumLevel.Override("Abp", LogEventLevel.Warning)

        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", applicationName);

    

    //END 9-FEB-26

    // Elasticsearch logging (same logic as ABP)
    var isElasticEnabled = context.Configuration.GetValue<bool>(
        context.Configuration["ElasticSearch:IsLoggingEnabled"]
    );

    if (isElasticEnabled)
    {
        loggerConfiguration.WriteTo.Elasticsearch(
            new ElasticsearchSinkOptions(
                new Uri(context.Configuration["ElasticSearch:Url"]!)
            )
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                IndexFormat = "tmsapi-log-{0:yyyy.MM}"
            }
        );
    }

    if (environment.IsDevelopment())
    {
        loggerConfiguration
            .WriteTo.Async(c => c.Console(
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"
            ))
            .WriteTo.Async(c => c.File(
                path: "Logs/logs.txt",
                rollingInterval: RollingInterval.Minute,
                retainedFileCountLimit: 7,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"
            ));
    }
    else
    {
        loggerConfiguration
            .WriteTo.Async(c => c.Console(new RenderedCompactJsonFormatter()));
    }
});

/* ---------------- SERVICES ---------------- */

// Add services to the container.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;

});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddHttpClient(); //200525 //Comment on 10-12-25

// WITH THIS: //Added on 10-12-25
builder.Services.AddHttpClient("Default", client =>
{
    client.DefaultRequestHeaders.ConnectionClose = false; // Keep-alive
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = 500,
    EnableMultipleHttp2Connections = true,
    ConnectTimeout = TimeSpan.FromSeconds(5),  // NEW
    ResponseDrainTimeout = TimeSpan.FromSeconds(2),  // NEW
    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
    KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
    UseProxy = false,  // NEW - explicit disable
    //ExpectContinueTimeout = TimeSpan.FromMilliseconds(300)  // NEW
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddUserContextValue();//Added on 10-JAN-26
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor(); // Add this line
builder.Services.AddScoped<PlatformService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<IDLConnection, DLConnection>();
builder.Services.AddScoped<IHotelXML, HotelXML>();
builder.Services.AddScoped<IAuthenticationTokenAgent, AuthenticationTokenAgent>();
builder.Services.AddScoped<IException, ExceptionLog>();
builder.Services.AddScoped<ISupplierRes, SupplierResRepo>();
builder.Services.AddScoped<ICommonMethod, CommonMethodRepo>();
builder.Services.AddScoped<IVCN, VCNRepo>();
builder.Services.AddScoped<IMarkup, MarkupRepo>();
builder.Services.AddScoped<ICacheMaster, CacheMasterRepo>();
builder.Services.AddScoped<ITokenReaderService, TokenReaderService>();

builder.Services.AddSupplierDependencies(); // <-- Call your extension here

builder.Services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
builder.Services.AddSingleton<RabbitMQPublisher>();
//builder.Services.AddSingleton<RadisConnection>();//Added on 14-AUG-2025
builder.Services.AddScoped<RadisConnection>();
builder.Services.AddScoped<IExceptionLogStore, MongoExceptionLogStore>(); //Added on 31-DEC-25


#region Supplier DI
//builder.Services.AddScoped<IAdonis, AdonisRepo>();
//builder.Services.AddScoped<IAgoda, AgodaRepository>();
////builder.Services.AddScoped<IBooking, BookingDotComRepoXML>();
//builder.Services.AddScoped<IBooking, BookingDotComRepo>();
//builder.Services.AddScoped<IDarina, DarinaRepo>();
//builder.Services.AddScoped<IDedge, DedgeRepository>();
//builder.Services.AddScoped<IDerbySoft, DerbySoftRepository>();
//builder.Services.AddScoped<IDida, DidaRepository>();
//builder.Services.AddScoped<IExpedia, ExpediaRepository>();
//builder.Services.AddScoped<IFitruums, FitruumsRepository>();
//builder.Services.AddScoped<IGoGlobal, GoGlobalRepo>();
//builder.Services.AddScoped<IGrnc, GrncRepo>();
//builder.Services.AddScoped<IHotelbeds, HotelbedsRepository>();
//builder.Services.AddScoped<IHoteldo, HoteldoRepository>();
//builder.Services.AddScoped<IHotelRunner, HotelRunnerRepository>();
//builder.Services.AddScoped<IHotelsPro, HotelsProRepo>();
//builder.Services.AddScoped<IHyperguest, HyperguestRepository>();
//builder.Services.AddScoped<IiBoosy, iBoosyRepository>();
//builder.Services.AddScoped<IInfiniteHotelsFrance, InfiniteHotelsFranceRepo>();
//builder.Services.AddScoped<IIwtx, IWTXRepository>();
//builder.Services.AddScoped<IJumeirah, JumeirahRepo>();
//builder.Services.AddScoped<IJuniper, JuniperRepo>();
//builder.Services.AddScoped<IMgbedbank, MgbedbankRepo>();
//builder.Services.AddScoped<INuitee, NuiteeRepo>();
//builder.Services.AddScoped<IOmnibees, OmnibeesRepo>();
//builder.Services.AddScoped<IOth, OthRepository>();
//builder.Services.AddScoped<IPaximum, PaximumRepo>();
//builder.Services.AddScoped<IQtech, QtechRepository>();
//builder.Services.AddScoped<IRakuten, RakutenRepo>();
//builder.Services.AddScoped<IRateGain, RateGainRepository>();
//builder.Services.AddScoped<IRateTiger, RateTigerRepository>();
//builder.Services.AddScoped<IRestel, RestelRepo>();
//builder.Services.AddScoped<ISiteMinder, SiteMinderRepository>();
//builder.Services.AddScoped<IStaah, StaahRepo>();
//builder.Services.AddScoped<ITbo, TboRepo>();
//builder.Services.AddScoped<ITravellanda, TravellandaRepo>();
//builder.Services.AddScoped<ITravelclick, TravelclickRepository>();
//builder.Services.AddScoped<ITraveloka, TravelokaRepository>();
//builder.Services.AddScoped<IUkTravco, UkTravcoRepo>();
//builder.Services.AddScoped<IWebbeds, WebbedsRepo>();
//builder.Services.AddScoped<IWebhoteliter, WebhoteliterRepo>();
//builder.Services.AddScoped<IAllXTG, XTGRepository>();
//builder.Services.AddScoped<IYalago, YalagoRepo>();

#endregion

#region Static Data
//SD.ConnectionString = builder.Configuration.GetConnectionString("ConnectionString");
//SD.ConnectionStringControl = builder.Configuration.GetConnectionString("ConnectionStringControl");
//SD.ConnectionStringBalancer = builder.Configuration.GetConnectionString("ConnectionStringBalancer");
//SD.ConnectionStringforlogtrack = builder.Configuration.GetConnectionString("ConnectionStringforlogtrack");
//SD.ConnectionString_SaveSupplierLog = builder.Configuration.GetConnectionString("ConnectionString_SaveSupplierLog");
//SD.ConnectionString_SupplierLog_report = builder.Configuration.GetConnectionString("ConnectionString_SupplierLog_report");

//SD.ConnectionString_ReplicationDB = builder.Configuration.GetConnectionString("ConnectionString_ReplicationDB");
//SD.ConnectionString_ReplicationDB1 = builder.Configuration.GetConnectionString("ConnectionString_ReplicationDB1");
//SD.ConnectionString_ReplicationDB2 = builder.Configuration.GetConnectionString("ConnectionString_ReplicationDB2");
//SD.ConnectionString_ReplicationDB3 = builder.Configuration.GetConnectionString("ConnectionString_ReplicationDB3");

//SD.ConnectionStringforMappingVervo = builder.Configuration.GetConnectionString("ConnectionStringforMappingVervo");
//SD.ConnectionStringforlogtrack_APILogVM = builder.Configuration.GetConnectionString("ConnectionStringforlogtrack_APILogVM");
//SD.ConnectionString_cm = builder.Configuration.GetConnectionString("ConnectionString_cm");

//SD.IsHoteIdRestriction = builder.Configuration["AppSettings:IsHoteIdRestriction"];
//SD.IsTimeCutOffActive = builder.Configuration["AppSettings:IsTimeCutOffActive"];
//SD.IsSaveSupplierSeachLog = builder.Configuration["AppSettings:IsSaveSupplierSeachLog"];
//SD.SSTimeout = builder.Configuration["AppSettings:SSTimeout"];
//SD.ProccessCutOffTime = builder.Configuration["AppSettings:ProccessCutOffTime"];
//SD.StoreSupplierResErrorLog = builder.Configuration["AppSettings:StoreSupplierResErrorLog"];
//SD.StoreSupplierResErrorLog_TBO_API = builder.Configuration["AppSettings:StoreSupplierResErrorLog_TBO_API"];
//SD.ResProccessTimerLogActive = builder.Configuration["AppSettings:ResProccessTimerLogActive"];
//SD.ResProccessTimeValueforSaveLog = builder.Configuration["AppSettings:ResProccessTimeValueforSaveLog"];
//SD.Use_ReplicationDB = builder.Configuration["AppSettings:Use_ReplicationDB"];
//SD.staticHoteldata = builder.Configuration["AppSettings:staticHoteldata"];
//SD.IsSubAgentCacheActive = builder.Configuration["AppSettings:IsSubAgentCacheActive"];
//SD.UseLocalDatabase = builder.Configuration["AppSettings:UseLocalDatabase"];
//SD.IsLive = builder.Configuration["AppSettings:IsLive"];
//SD.B2CMarkup = builder.Configuration["AppSettings:B2CMarkup"];
//SD.SaveNoCountSupplog = builder.Configuration["AppSettings:SaveNoCountSupplog"];
//SD.SaveSupplier_Error_LogActive_95 = builder.Configuration["AppSettings:SaveSupplier_Error_LogActive_95"];
//SD.IsWithinearth = builder.Configuration["AppSettings:IsWithinearth"];
//SD.SupplierPerformanceReport = builder.Configuration["AppSettings:SupplierPerformanceReport"];
//SD.OtherAgent_Timeout = builder.Configuration["AppSettings:OtherAgent_Timeout"];
//SD.BackgroundLogSave = builder.Configuration["AppSettings:BackgroundLogSave"];
//SD.xconnectCache = builder.Configuration["AppSettings:xconnectCache"];
//SD.SERX_AMTJ_Timeout = builder.Configuration["AppSettings:SERX_AMTJ_Timeout"];

//SD.MongoDB_Active_for_Cachefile = builder.Configuration["MongoSettings:MongoDB_Active_for_Cachefile"];
//SD.MongoDB_ConnectionStr = builder.Configuration["MongoSettings:MongoDB_ConnectionStr"];
//SD.MongoDB_DatabaseName = builder.Configuration["MongoSettings:MongoDB_DatabaseName"];
//SD.MongoDB_CollectionName = builder.Configuration["MongoSettings:MongoDB_CollectionName"];
//SD.MongoDB_ConnectionStr_SupplierLog = builder.Configuration["MongoSettings:MongoDB_ConnectionStr_SupplierLog"];
//SD.MongoDB_CollectionName_SuppLog = builder.Configuration["MongoSettings:MongoDB_CollectionName_SuppLog"];

#endregion

// Register MongoDB connection as a singleton service
//builder.Services.AddSingleton<MongoDbFactory>(); //200525
builder.Services.AddSingleton<MongoDbFactoryNew>(); //030625

builder.Services.Configure<KeySettings>(builder.Configuration.GetSection("KeySettings"));
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.Configure<RabitMQSettings>(builder.Configuration.GetSection("RabitMQSettings"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));//Added on 14-AUG-2025
builder.Services.Configure<RemoteServices>(builder.Configuration.GetSection("RemoteServices"));//Added on 09-FEB-26

var app = builder.Build();

// WARMUP MONGO BEFORE SERVING REQUESTS
using (var scope = app.Services.CreateScope())
{
    var mongo = scope.ServiceProvider.GetRequiredService<MongoDbFactoryNew>();
    await mongo.WarmUpMongoAsync();   // <--- IMPORTANT
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseUserContextValue("HotelServices", "Development"); //Added on 10-Jan-26
}
else
{
    app.UseUserContextValue("HotelServices", "Production");
}
app.UseHttpsRedirection();

app.UseSerilogRequestLogging(); // <-- Important (ABP does this internally)

app.UseAuthorization();

app.MapControllers();

app.Run();
