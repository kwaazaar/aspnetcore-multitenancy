using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Twygger.Config
{
    public class ConfigEntry<T>
        where T: class, new()
    {
        private readonly string _sectionName;
        public ConfigEntry(string sectionName)
        {
            _sectionName = sectionName;
        }

        public void Configure(IServiceCollection services, IConfiguration config)
        {
            Configure(services, string.Empty, config);
        }

        public void Configure(IServiceCollection services, string name, IConfiguration config)
        {
            services.Configure<T>(config.GetSection(_sectionName));
        }
    }
}
