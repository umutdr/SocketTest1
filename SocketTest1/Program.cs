using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketTest1.Models;
using System.IO;
using System.Threading.Tasks;

namespace SocketTest1
{
    class Program
    {
        private static IHostBuilder HostBuilder
        {
            get
            {
                var config
                    = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();

                var hostBuilder
                    = Host.CreateDefaultBuilder()
                            .ConfigureServices((context, serviceCollection) =>
                            {
                                XSettingsModel xSettingsModel = new XSettingsModel();
                                var appSettingsXSettingsSection = config.GetSection("X");
                                appSettingsXSettingsSection.Bind(xSettingsModel);

                                #region Options
                                serviceCollection.AddOptions();
                                serviceCollection.Configure<XSettingsModel>(appSettingsXSettingsSection);
                                #endregion

                                serviceCollection
                                    .AddHostedService<SocketServerBackgroundService>()
                                    .AddSingleton<ISocketServer, SocketServer>();
                            });

                return hostBuilder;
            }
        }

        static async Task Main(string[] args)
        {
            var host = HostBuilder.Build();
            await host.RunAsync();
        }
    }
}