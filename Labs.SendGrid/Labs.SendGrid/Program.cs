using Labs.SendGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SendGrid.Extensions.DependencyInjection;
using Serilog;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configBuilder =>
    {
        var configuration = configBuilder.AddJsonFile("appsettings.json", false)
                   .AddJsonFile($"appsettings.development.json", true)
                   .AddCommandLine(args)
                   .Build();

        Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithThreadId()
                .CreateLogger();
    })
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        services.AddSendGrid((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetService(typeof(IConfiguration)) as IConfiguration;

            options.ApiKey = configuration.GetValue("SendGrid:ApiKey", string.Empty);
        });

        services.AddHostedService<Worker>();
    });

var host = builder.Build();

host.Run();
