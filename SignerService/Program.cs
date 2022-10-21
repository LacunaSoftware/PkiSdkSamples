using Lacuna.SignerService;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SignerService {
	 public class Program {
		  public static void Main(string[] args) {
				IHost host = Host.CreateDefaultBuilder(args)
					 .ConfigureServices(services => {
						  services.AddHostedService<DirectoryWatcher>();
						  services.AddSingleton<DocumentService>();
					 })
					 .Build();
				host.Run();
		  }
	 }
}
