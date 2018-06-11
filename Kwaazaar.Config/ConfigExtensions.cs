using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Kwaazaar.Config
{
    public static class ConfigExtensions
    {
        private static bool _enabled = false;
        private static ICustomizedTenantsSet _customizedTenants = null;
        private static Dictionary<string, IConfigurationSection> _customizedTenantsDictionary = null;

        public static IServiceCollection EnableMultiTenancySupport(this IServiceCollection services, IConfiguration config)
        {
            if (_enabled) throw new InvalidOperationException("MultiTenancySupport has already been enabled");

            // Ensure support for AspNetCore's configuration options
            services.AddOptions();

            // Create set for holding registration of customized tenants
            _customizedTenants = new CustomizedTenantsSet();
            _customizedTenantsDictionary = new Dictionary<string, IConfigurationSection>();

            var customizedTenantsSection = config.GetSection("CustomizedTenants");
            if (customizedTenantsSection.Exists())
            {
                foreach (var tenant in customizedTenantsSection.GetChildren())
                {
                    // Customized tenants must have their properties ALWAYS be fully customized, so for non-customized configuration load the default (unnamed) configuration
                    var tenantId = tenant.Key;
                    _customizedTenants.AddTenantId(tenantId);
                    _customizedTenantsDictionary.Add(tenantId, tenant);
                }
            }

            // Make sure the DI-action can access the list of customized tenants
            services.AddSingleton(_customizedTenants);
            _enabled = true;

            return services;
        }


        /// <summary>
        /// Add Twygger configuration options support (DI-registered, auto-reloading configuration models) with multi-tenancy support
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="config">IConfiguration to load from</param>
        /// <param name="sectionName">Optional: sectionName for where to find the configuration from. When not specified, the classname will be used as sectionName.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AutoConfigure<TModel>(this IServiceCollection services, IConfiguration config, string sectionName = null)
            where TModel: class, new()
        {
            if (!_enabled) throw new InvalidOperationException("You must first call EnableMultiTenancySupport");

            var sectionNameToUse = sectionName ?? typeof(TModel).Name;

            // Configure the default/non-tenant specific model
            services.Configure<TModel>(Options.DefaultName, config.GetSection(sectionNameToUse));

            // Configure the tenant-specific model (only for customized tenants)
            foreach (KeyValuePair<string, IConfigurationSection> tenant in _customizedTenantsDictionary)
            {
                var tenantSpecificModelSection = tenant.Value.GetSection(sectionNameToUse); // See if the tenant-specific config contains this section
                services.Configure<TModel>(tenant.Key, tenantSpecificModelSection.Exists() ? tenant.Value.GetSection(sectionNameToUse) : config.GetSection(sectionNameToUse));
            }

            // Register the DI retrieval
            services.AddScoped<TModel>(sp => // Scoped lifetime (ASP.Net creates scope for every request, non-scoped attempts will fail)
            {
                // This code is executed when DI tries to resolve a constructor parameter
                var currentTenantId = sp.GetService<ITenantIdProvider>().GetTenantId();

                // This custom action retrieves an IOptionsSnapshot<TModel>, so that the value will be auto-reloaded on live config changes
                var obj = sp.GetService<ICustomizedTenantsSet>().ContainsTenantId(currentTenantId)
                    ? sp.GetService<IOptionsSnapshot<TModel>>().Get(currentTenantId) // IOptionsSnapshot<T> will always return a value (empty instance when there is nothing configured for this tenant)
                    : sp.GetService<IOptionsSnapshot<TModel>>().Value;

                // If it derives from out ConfigModel, we can support additional calls for validation and setting the tenant
                if (obj != null)
                {
                    if (obj is ConfigModel tc)
                    {
                        tc.SetTenantId(currentTenantId);
                        tc.Validate();
                    }
                }

                return obj;
            });

            return services;
        }
    }
}
