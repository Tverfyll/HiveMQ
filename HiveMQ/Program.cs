using Microsoft.Extensions.Options;
using MqttToPlantScada;
using MqttToPlantScada.Settings;
using Serilog;



var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log.txt");

Log.Logger = new LoggerConfiguration()
     .MinimumLevel.Debug()
     .WriteTo.Console()
     .WriteTo.File(
            path: path,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollingInterval: RollingInterval.Day, // 10 MB
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: 10)
     .CreateLogger();

try
{
    Log.Information("Starting host...");

    IHost host = Host.CreateDefaultBuilder(args)

        .UseWindowsService(options =>
        {
            options.ServiceName = "OneCoPlantScadaToMQTT";
        })


        .ConfigureAppConfiguration((hostContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true);
        })


        .ConfigureServices((hostContext, services) =>
        {

            services.Configure<MqttSettings>(hostContext.Configuration.GetSection(nameof(MqttSettings)));

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<MqttSettings>>().Value);

            services.AddHostedService<Worker>();
        })
        .UseSerilog()
        .Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
