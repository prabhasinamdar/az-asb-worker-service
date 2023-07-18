using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using WorkerService.ServiceBus;
using Serilog;
using Azure.Messaging.ServiceBus;
using WorkerService.ServiceBus.Contracts;
using WorkerService.ServiceBus.Helper;
using WorkerService.ServiceBus.Models;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = ".NET ASB Service";
    })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var env = hostingContext.HostingEnvironment;
        // optional extra provider
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables(); // overwrites previous values
    })
    .ConfigureServices((context, services) =>
    {
        //LoggerProviderOptions.RegisterProviderOptions<
        //   EventLogSettings, EventLogLoggerProvider>(services);
        IConfiguration configuration = context.Configuration;
        AppSettingsModel options = configuration.GetSection("AppSettings").Get<AppSettingsModel>();

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddScoped<IServiceBusMessage<ServiceBusReceivedMessage>, ServiceBusMessage<ServiceBusReceivedMessage>>();
        services.AddScoped<IServiceBusMessage<ServiceBusMessageDto>, ServiceBusMessage<ServiceBusMessageDto>>();
        services.AddScoped<IServiceBusMessage<ServiceBusClientConfigDto>, ServiceBusMessage<ServiceBusClientConfigDto>>();
        services.AddScoped<IApiHelper, ApiHelper>();
        

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddSerilog();
        });

        services.AddHostedService<Worker>();

        // See: https://github.com/dotnet/runtime/issues/47303
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(
                context.Configuration.GetSection("Logging"));
        });
    });

IHost host = builder.Build();
host.Run();
