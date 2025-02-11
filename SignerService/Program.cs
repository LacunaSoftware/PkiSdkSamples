using Lacuna.SignerService;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;

namespace SignerService {
    public class Program {
        public static void Main(string[] args) {
            IHost host = Host.CreateDefaultBuilder(args)
                .UseWindowsService(options => {
                    options.ServiceName = "LacunaSignerService";
                })
                .ConfigureServices(services => {
                    LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
                    services.AddHostedService<DirectoryWatcher>();
                    services.AddSingleton<DocumentService>();
                })
                .ConfigureLogging((context, logging) => {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                })
                .UseSerilog((hostingContext, services, loggerConfiguration) => {
                    var errorLogFilePath = hostingContext.Configuration.GetValue<string>("LogErrorPath", "error.txt");
                    loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.File(errorLogFilePath, Serilog.Events.LogEventLevel.Error);

                    }
                )
                .Build();
            host.Run();
        }
    }
}
