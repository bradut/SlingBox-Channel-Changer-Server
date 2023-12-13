using Application.Abstractions;
using Application.Services;
using RunSlingServer.Services.SignalR;
using RunSlingServer.WebApi.Services.EndpointsServices;
using RunSlingServer.WebApi.Services.Interfaces;
using SignalRNotifier = RunSlingServer.Services.SignalR.SignalRNotifier;

namespace RunSlingServer.WebApi.Services
{
    public class WebApiService
    {
        private readonly ConsoleDisplayDispatcher _console;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly SignalRNotifier? _signalRNotifier;

        private readonly string _serverRootPath;
        private readonly string[] _args;
        private ILogger _logger;



        public WebApiService(string[] args, string serverRootPath,
                             ConsoleDisplayDispatcher console,
                             IFileSystemAccess fileSystemAccess,
                             SignalRNotifier? signalRNotifier,
                             ILogger logger 
                             )
        {
            _args = args;
            _console = console;
            _serverRootPath = serverRootPath;
            _fileSystemAccess = fileSystemAccess;
            _signalRNotifier = signalRNotifier;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            await Task.Run(() =>
            {
                var app = CreateApplication(_args);
                app.Run();
            });
        }

        private WebApplication CreateApplication(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            var environmentName = builder.Environment.EnvironmentName;
            var developmentFileName = $"appsettings.{environmentName}.json";
            
            var configuration = new ConfigurationBuilder()
            .SetBasePath(_serverRootPath)
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(developmentFileName, optional: true, reloadOnChange: true)
            .Build();

            builder.WebHost.UseConfiguration(configuration);
            builder.Configuration.AddConfiguration(configuration);
            builder.WebHost.ConfigureKestrel((context, serverOptions) =>
            {
                var contextConfiguration = context.Configuration;
                var httpUrl = contextConfiguration.GetSection("Kestrel:Endpoints:Http:Url").Value;
                var httpsUrl = contextConfiguration.GetSection("Kestrel:Endpoints:Https:Url").Value;

                if (!string.IsNullOrEmpty(httpUrl))
                {
                    serverOptions.ListenAnyIP(int.Parse(httpUrl.Split(':')[2]), _ =>
                    {
                        // Configure the Http endpoint, if needed.
                    });
                }

                //if (!string.IsNullOrEmpty(httpsUrl))
                //{
                //    serverOptions.ListenAnyIP(int.Parse(httpsUrl.Split(':')[2]), _ =>
                //    {

                //    });
                //}

                if (!string.IsNullOrEmpty(httpsUrl))
                {
                    var urlParts = httpsUrl.Split(':');
                    if (urlParts.Length == 3)
                    {
                        serverOptions.ListenAnyIP(int.Parse(urlParts[2]), _ =>
                        {
                            // Configure the Https endpoint, if needed.
                        });
                    }
                }
            });


            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new CustomConsoleLoggerProvider(_console));

            builder.Services.AddSingleton<ILogger>(_ => new CustomConsoleLogger(_console));
            builder.Services.AddSingleton(_fileSystemAccess);



            // Apply the startup class ConfigureServices
            ConfigureServices(builder.Services);

            // Build the App for the Configure section of startup class
            var webApplication = builder.Build();

            // Apply the startup class Configure
            Configure(app: webApplication, env: webApplication.Environment, logger: webApplication.Services.GetService<ILogger<WebApiService>>()!);

            return webApplication;
        }


        // Dependency Injection
        private void ConfigureServices(IServiceCollection services) //, IConfiguration configuration
        {
            services.AddSingleton<ConsoleDisplayDispatcher>();
            services.AddHttpClient();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var consoleControlService = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();
                return new CustomConsoleLoggerProvider(consoleControlService);
            });

            services.AddSignalR();

            services.AddSingleton<IWebHelpers, WebHelpers>();
         
            services.AddSingleton<IPostToUrlHandler, PostToUrlHandler>(serviceProvider =>
             {
                    var consoleDisplayDispatcher = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();
                    var webHelper = serviceProvider.GetRequiredService<IWebHelpers>();

                    return new PostToUrlHandler(consoleDisplayDispatcher, _fileSystemAccess, _signalRNotifier, _logger, webHelper);
            });

            services.AddSingleton<IGetStreamingStatusHandler, GetStreamingStatusHandler>(serviceProvider =>
            {
                var consoleDisplayDispatcher = serviceProvider.GetRequiredService<ConsoleDisplayDispatcher>();

                return new GetStreamingStatusHandler(consoleDisplayDispatcher, _fileSystemAccess, _logger);
            });

           
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSignalR",
                    builder =>
                    {
                        builder
                            // Allow any origin dynamically while still allowing credentials to be included in the request
                            .SetIsOriginAllowed(_ => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
        }

        // Configure registered services above
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<WebApiService> logger)
        {
            _logger = logger;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors("AllowSignalR"); // Apply the "AllowSignalR" CORS policy to the app

            // Add Logging
            _ = app.Use(async (context, next) =>
            {
                logger.LogInformation($"Request {context.Request.Path.Value} received.");
                await next();
                logger.LogInformation($"Response {context.Response.StatusCode} returned for {context.Request.Path.Value}.");
            });


            MapEndpoints(app);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "<Pending>")]
        public void MapEndpoints(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {

                // API endpoints
                endpoints.MapGet("/api/streamingstatus",
                async Task<string> (HttpContext context, IGetStreamingStatusHandler streamingStatusService) =>
                {
                    return await streamingStatusService.GetStreamingStatus(context);
                });



                endpoints.MapPost("/api/post-to-url", 
                    async Task<string> (HttpRequest request, IPostToUrlHandler postToUrlHandler) =>
                {
                    return await postToUrlHandler.HandlePostToUrl(request);

                });



                // SignalR endpoint
                endpoints.MapHub<SignalRAuctionHub>("/auctionhub");


                // Disable CORS
                app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
        }
    }
}


