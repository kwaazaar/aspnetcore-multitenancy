using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using Winton.Extensions.Configuration.Consul;
using Microsoft.AspNetCore.Http;

namespace MultiTenantWeb
{
    public class Startup
    {
        private readonly IConfiguration Configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private HashSet<string> _customizedTenants = new HashSet<string>();

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.{env}.json", optional: true, reloadOnChange: true)
                .AddConsul($"shared", _cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                .AddConsul($"ProfileService", _cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                .AddConsul($"CustomizedTenants", _cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; o.Optional = true; })
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // WebAPI/Mvc
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMvc();

            // Config DI setup
            services.AddOptions();

            // DbConfig
            services.Configure<DbConfig>(Configuration.GetSection("DbConfig")); // Default (unnamed) for non-customized tenants

            if (Configuration.GetValue<bool>("CustomizedTenantsLoaded", false))
            {
                foreach (var tenant in Configuration.GetSection("CustomizedTenants").GetChildren().ToList())
                {
                    // Customized tenants must ALWAYS be fully customized, so for non-customized configuration load the default (unnamed) configuration
                    var tenantId = tenant.Key;

                    _customizedTenants.Add(tenantId);

                    // DbConfig
                    var dbConfigSection = tenant.GetSection("DbConfig");
                    services.Configure<DbConfig>(tenantId, dbConfigSection.Exists() ? dbConfigSection : Configuration.GetSection("DbConfig"));
                }
            }

            services.AddScoped<DbConfig>(sp => {
                var tenantId = GetTenantId();
                var dbConfig = _customizedTenants.Contains(tenantId) ? sp.GetService<IOptionsSnapshot<DbConfig>>().Get(tenantId) : sp.GetService<IOptionsSnapshot<DbConfig>>().Value;
                if (dbConfig != null)
                {
                    dbConfig.Server = dbConfig.Server?.Replace("%tenantid%", tenantId);
                    dbConfig.Database = dbConfig.Database?.Replace("%tenantid%", tenantId);
                }
                return dbConfig;
            });
        }

        private string GetTenantId()
        {
            var ctx = new HttpContextAccessor().HttpContext;
            return (ctx?.Request != null)
                ? ctx.Request.Headers.SingleOrDefault(h => h.Key == "X-TenantId").Value.FirstOrDefault() ?? string.Empty
                : null;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            appLifetime.ApplicationStopping.Register(_cancellationTokenSource.Cancel);

            app.UseMvc();
        }
    }
}
