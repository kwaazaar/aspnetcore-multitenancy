using Kwaazaar.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace MultiTenantWeb
{
    public class Startup
    {
        private readonly IConfiguration Configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.custom.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // WebAPI/Mvc
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); // Required by HttpRequestTenantIdProvider, which is used to determine the tenantid
            services.AddMvc();

            // Config DI setup
            services.AutoConfigure(Configuration, typeof(DbConfig), typeof(CustomConfig));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Required for cancelling the cancellationtoken supplied to AddTwyggerConfigSources
            appLifetime.ApplicationStopping.Register(_cancellationTokenSource.Cancel);

            app.UseMvc();
        }
    }
}
