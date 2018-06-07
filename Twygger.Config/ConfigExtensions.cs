using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Kwaazaar.Config
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Add Twygger configuration options support (DI-registered, auto-reloading configuration models) with multi-tenancy support
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="config">IConfiguration to load from</param>
        /// <param name="configModels">Types of configuration models that must be loaded (each required model must be explicitly specified here).</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AutoConfigure(this IServiceCollection services, IConfiguration config, params Type[] configModels)
        {
            return AutoConfigure(services, config, tenantCustomizationEnabled: true, tenantIdProvider: null, configModels: configModels);
        }

        /// <summary>
        /// Add Twygger configuration options support (DI-registered, auto-reloading configuration models) with multi-tenancy support
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="config">IConfiguration to load from</param>
        /// <param name="tenantCustomizationEnabled">Optional: when enabled this allowes for tenant-specific customizations (other than tenantid-variable replacements, etc), default: enabled</param>
        /// <param name="configModels">Types of configuration models that must be loaded (each required model must be explicitly specified here).</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AutoConfigure(this IServiceCollection services, IConfiguration config, bool tenantCustomizationEnabled = true, params Type[] configModels)
        {
            return AutoConfigure(services, config, tenantCustomizationEnabled: tenantCustomizationEnabled, tenantIdProvider: null, configModels: configModels);
        }

        /// <summary>
        /// Add Twygger configuration options support (DI-registered, auto-reloading configuration models) with multi-tenancy support
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="config">IConfiguration to load from</param>
        /// <param name="tenantCustomizationEnabled">Optional: when enabled this allowes for tenant-specific customizations (other than tenantid-variable replacements, etc), default: enabled</param>
        /// <param name="tenantIdProvider">Optional: specifies the provider that supplies the tenant-id at any required moment (default: HttpRequestTenantIdProvider)</param>
        /// <param name="configModels">Types of configuration models that must be loaded (each required model must be explicitly specified here).</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AutoConfigure(this IServiceCollection services, IConfiguration config, bool tenantCustomizationEnabled = true, ITenantIdProvider tenantIdProvider = null, params Type[] configModels)
        {
            // Ensure support for AspNetCore's configuration options
            services.AddOptions();

            // Inject the tenantid provider
            services.AddSingleton(tenantIdProvider ?? new HttpRequestTenantIdProvider());

            // Create set for holding registration of customized tenants
            ICustomizedTenantsSet customizedTenants = new CustomizedTenantsSet();
            var customizedTenantsDictionary = new Dictionary<string, IConfigurationSection>();

            // Determine customized tenants
            if (tenantCustomizationEnabled)
            {
                var customizedTenantsSection = config.GetSection("CustomizedTenants");
                if (customizedTenantsSection.Exists())
                {
                    foreach (var tenant in customizedTenantsSection.GetChildren())
                    {
                        // Customized tenants must have their properties ALWAYS be fully customized, so for non-customized configuration load the default (unnamed) configuration
                        var tenantId = tenant.Key;
                        customizedTenants.AddTenantId(tenantId);
                        customizedTenantsDictionary.Add(tenantId, tenant);
                    }
                }
            }

            // Configure & register configuration models
            var modelTypeInstances = new Dictionary<Type, ConfigModel>();
            foreach (var modelType in configModels)
            {
                if (!modelType.IsSubclassOf(typeof(ConfigModel)))
                    throw new ArgumentException($"Twygger ConfigModel {modelType.Name} must inherit from {nameof(ConfigModel)}");

                // Instantiate the modelType, so that we can call some of its methods to properly set it up
                var modelTypeInstance = (ConfigModel)Activator.CreateInstance(modelType);
                modelTypeInstances.Add(modelType, modelTypeInstance);

                // Configure for unnamed queries (for non-customized tenants)
                modelTypeInstance.ConfigureModel(services, Options.DefaultName, config);

                // Configure for customized tenants
                if (tenantCustomizationEnabled)
                {
                    foreach (KeyValuePair<string, IConfigurationSection> tenant in customizedTenantsDictionary)
                    {
                        var tenantSpecificModelSection = tenant.Value.GetSection(modelTypeInstance.SectionName); // See if the tenant-specific config contains this section
                        modelTypeInstance.ConfigureModel(services, tenant.Key, tenantSpecificModelSection.Exists() ? tenant.Value : config);
                    }
                }

                // Register the DI retrieval
                modelTypeInstance.RegisterModel(services);
            }

            // Make sure the DI-action can access the list of customized tenants
            services.AddSingleton(customizedTenants);

            return services;
        }
    }
}
