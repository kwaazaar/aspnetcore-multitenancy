using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using Winton.Extensions.Configuration.Consul;

namespace ConfigConsul
{
    public class Startup
    {
        public readonly IConfigurationRoot Configuration;
        private readonly CancellationTokenSource CancellationTokenSource;

        public Startup(CancellationTokenSource cancellationTokenSource, string env = null, string[] args = null)
        {
            CancellationTokenSource = cancellationTokenSource;

            var builder = new ConfigurationBuilder()
							
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile("appsettings.{env}.json", optional: true, reloadOnChange: true)
                            .AddConsul($"shared", CancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                            .AddConsul($"{env}/shared", CancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; o.Optional = true; })
                            .AddConsul($"{env}/configdemo", CancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; o.Optional = true; })
                            .AddConsul($"tenantconfigs", CancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; o.Optional = true; })
                            .AddEnvironmentVariables()
                            .AddCommandLine(args);

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
			// Config DI setup
            serviceCollection.AddOptions();
            serviceCollection.Configure<ServiceConfig>(Configuration.GetSection("ServiceConfig"));

            serviceCollection.Configure<ServiceConfig>(sc => {
                
					sc.Environment = sc.Environment.Replace("%ticks%", DateTime.Now.Ticks.ToString());
            }); // Postprocessing of loaded config

            serviceCollection.AddScoped(sp => sp.GetService<IOptionsSnapshot<ServiceConfig>>().Value);

			// Service/controller DI setup
            serviceCollection.AddScoped<Controller>();
        }
    }
}
