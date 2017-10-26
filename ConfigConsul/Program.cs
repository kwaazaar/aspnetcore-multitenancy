using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;

namespace ConfigConsul
{
    class Program
    {
        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var startup = new Startup(cancellationTokenSource, "test", args);

            var serviceCollection = new ServiceCollection();
            startup.ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            for (var i = 0; i < 100;i++)
            {
                // Simulate multiple request coming into webservice
                using (var scope = serviceProvider.CreateScope()) // Nesseccary for console apps (web-apps have new scopes for every request)
                {
                    var ctrl = scope.ServiceProvider.GetService<Controller>();
                    ctrl.DoStuff();
                }

                Thread.Sleep(1000); // Change value during this sleep and ReloadOnChange will reload it
            }

            cancellationTokenSource.Cancel();
        }
    }
}
