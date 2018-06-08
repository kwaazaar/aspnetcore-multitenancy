using Kwaazaar.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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

            var tasks = new Task[] { ReadDbConfig("t1"), ReadDbConfig("t2"), ReadDbConfig("t3"), ReadDbConfig("t4") };
            Task.WaitAll(tasks);

            /*
            var tenantIdProvider = (ContextTenantProvider)ServiceProvider.GetService<ITenantIdProvider>();

            var tenantId = "t1";
            tenantIdProvider.SetTenantId(tenantId);
            Console.WriteLine($"TenantId: {ServiceProvider.GetService<ITenantIdProvider>().GetTenantId()}");
            using (var spScope = ServiceProvider.CreateScope())
            {
                Console.WriteLine($"TenantId-In-Scope: {ServiceProvider.GetService<ITenantIdProvider>().GetTenantId()}");
                var dbConfig = spScope.ServiceProvider.GetService<DbConfig>();
                Console.WriteLine($"Server: {dbConfig.Server}, Database: {dbConfig.Database}, Username: {dbConfig.Username}, Password: {dbConfig.Password}");
            }

            tenantId = "t2";
            tenantIdProvider.SetTenantId(tenantId);
            Console.WriteLine($"TenantId: {ServiceProvider.GetService<ITenantIdProvider>().GetTenantId()}");
            using (var spScope = ServiceProvider.CreateScope())
            {
                Console.WriteLine($"TenantId-In-Scope: {ServiceProvider.GetService<ITenantIdProvider>().GetTenantId()}");
                var dbConfig = spScope.ServiceProvider.GetService<DbConfig>();
                Console.WriteLine($"Server: {dbConfig.Server}, Database: {dbConfig.Database}, Username: {dbConfig.Username}, Password: {dbConfig.Password}");
            }
            */
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
