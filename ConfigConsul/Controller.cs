using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigConsul
{
    public class Controller
    {
        private readonly ServiceConfig _serviceConfig;

        public Controller(ServiceConfig serviceConfig)
        {
            _serviceConfig = serviceConfig;
        }

        public void DoStuff()
        {
            Console.WriteLine($"Environment: {_serviceConfig.Environment}, IsMultiTenant: {_serviceConfig.IsMultiTenant}");
        }
    }
}
