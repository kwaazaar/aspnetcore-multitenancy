using Kwaazaar.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTenantConsole
{
    class Program
    {

        private static IServiceProvider ServiceProvider;

        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var tasks = new List<Task>();
            for (int i = 1; i < 100; i++)
                tasks.Add(ReadDbConfig("t" + i.ToString()));
            Task.WaitAll(tasks.ToArray());
        }

        private static async Task ReadDbConfig(string tenantId)
        {
            var tenantIdProvider = (ContextTenantProvider)ServiceProvider.GetService<ITenantIdProvider>();
            await Task.Delay(DateTime.UtcNow.Millisecond);
            tenantIdProvider.SetTenantId(tenantId);
            await Task.Delay(DateTime.UtcNow.Millisecond);

            Console.WriteLine($"TenantId({tenantId}-{Thread.CurrentThread.ManagedThreadId}): {ServiceProvider.GetService<ITenantIdProvider>().GetTenantId()}");
            await Task.Delay(DateTime.UtcNow.Millisecond);
            using (var spScope = ServiceProvider.CreateScope())
            {
                Console.WriteLine($"TenantId-In-Scope({tenantId}-{Thread.CurrentThread.ManagedThreadId}): {ServiceProvider.GetService<ITenantIdProvider>().GetTenantId()}");
                await Task.Delay(DateTime.UtcNow.Millisecond);
                var dbConfig = spScope.ServiceProvider.GetService<DbConfig>();
                Console.WriteLine($"({tenantId}-{Thread.CurrentThread.ManagedThreadId}) - Server: {dbConfig.Server}, Database: {dbConfig.Database}, Username: {dbConfig.Username}, Password: {dbConfig.Password}");
                if (dbConfig.Username != "Username-" + tenantId) throw new Exception("Mismatch!");
            }
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            var config = builder.Build();

            // Add IConfiguration dependency (reason: allows access to config from any injected component)
            serviceCollection.AddSingleton<IConfiguration>(config);
            serviceCollection.AddSingleton(typeof(ITenantIdProvider), typeof(ContextTenantProvider));

            // Configuration injection
            serviceCollection.AutoConfigure(config, typeof(DbConfig));
        }
    }
}
