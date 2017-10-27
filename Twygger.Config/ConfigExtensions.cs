using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using Winton.Extensions.Configuration.Consul;

namespace Twygger.Config
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Adds configsources desired/required for Twygger
        /// </summary>
        /// <param name="configBuilder">IConfigurationBuilder</param>
        /// <param name="cancellationTokenSource">CancellationTokenSource to be used for sources that support cancelling (eg. required for auto-reloading support for Consul)</param>
        /// <param name="consulAppConfigKey">Consul path to configuration specific for current application, eg: "ProfileService"</param>
        /// <param name="consulSharedConfigKey">Optional: consul path to shared configuration (default: "shared")</param>
        /// <param name="consulCustomizedTenantsConfigKey">Optional: consul path to tenant-specific customization (default: "CustomizedTenants")</param>
        /// <returns>IConfigurationBuilder</returns>
        public static IConfigurationBuilder AddTwyggerConfigSources(this IConfigurationBuilder configBuilder, CancellationTokenSource cancellationTokenSource,
            string consulAppConfigKey, string consulSharedConfigKey = "shared", string consulCustomizedTenantsConfigKey = "CustomizedTenants")
        {
            return configBuilder.AddConsul(consulSharedConfigKey, cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                .AddConsul(consulAppConfigKey, cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                .AddConsul(consulCustomizedTenantsConfigKey, cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; o.Optional = true; });
        }

        /// <summary>
        /// Add Twygger configuration options support (DI-registered, auto-reloading configuration models) with multi-tenancy support
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="config">IConfiguration to load from</param>
        /// <param name="configModels">Types of configuration models that must be loaded (each required model must be explicitly specified here).</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddTwyggerOptions(this IServiceCollection services, IConfiguration config, params Type[] configModels)
        {
            return AddTwyggerOptions(services, config, tenantCustomizationEnabled: true, tenantIdProvider: null, configModels: configModels);
        }

        /// <summary>
        /// Add Twygger configuration options support (DI-registered, auto-reloading configuration models) with multi-tenancy support
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="config">IConfiguration to load from</param>
        /// <param name="tenantCustomizationEnabled">Optional: when enabled this allowes for tenant-specific customizations (other than tenantid-variable replacements, etc), default: enabled</param>
        /// <param name="configModels">Types of configuration models that must be loaded (each required model must be explicitly specified here).</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddTwyggerOptions(this IServiceCollection services, IConfiguration config, bool tenantCustomizationEnabled = true, params Type[] configModels)
        {
            return AddTwyggerOptions(services, config, tenantCustomizationEnabled: tenantCustomizationEnabled, tenantIdProvider: null, configModels: configModels);
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
        public static IServiceCollection AddTwyggerOptions(this IServiceCollection services, IConfiguration config, bool tenantCustomizationEnabled = true, ITenantIdProvider tenantIdProvider = null, params Type[] configModels)
        {
            // Add support for AspNetCore's configuration options
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
            var modelTypeInstances = new Dictionary<Type, TwyggerConfig>();
            foreach (var modelType in configModels)
            {
                if (!modelType.IsSubclassOf(typeof(TwyggerConfig)))
                    throw new ArgumentException($"Twygger ConfigModel {modelType.Name} must inherit from {nameof(TwyggerConfig)}");

                var modelTypeInstance = (TwyggerConfig)Activator.CreateInstance(modelType);
                modelTypeInstances.Add(modelType, modelTypeInstance);

                // Configure for unnamed queries (for non-customized tenants)
                modelTypeInstance.ConfigureModel(services, Options.DefaultName, config);

                // Configure for customized tenants
                if (tenantCustomizationEnabled)
                {
                    foreach (KeyValuePair<string, IConfigurationSection> tenant in customizedTenantsDictionary)
                    {
                        var tenantSpecificModelSection = tenant.Value.GetSection(modelTypeInstance.SectionName);
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
