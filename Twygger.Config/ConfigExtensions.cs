using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Winton.Extensions.Configuration.Consul;

namespace Twygger.Config
{
    public static class ConfigExtensions
    {
        public static IConfigurationBuilder AddTwyggerConfigSources(this IConfigurationBuilder configBuilder, CancellationTokenSource cancellationTokenSource,
            string consulAppConfigKey, string consulSharedConfigKey = "shared", string consulCustomizedTenantsConfigKey = "CustomizedTenants")
        {
            return configBuilder.AddConsul(consulSharedConfigKey, cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                .AddConsul(consulAppConfigKey, cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; })
                .AddConsul(consulCustomizedTenantsConfigKey, cancellationTokenSource.Token, (o) => { o.ReloadOnChange = true; o.Optional = true; });
        }

        public static IServiceCollection AddTwyggerOptions(this IServiceCollection services, IConfiguration config, ITenantIdProvider tenantIdProvider = null, params Type[] configModels)
        {
            // Add support for AspNetCore's configuration options
            services.AddOptions();

            // Inject the tenantid provider
            services.AddSingleton(tenantIdProvider ?? new HttpRequestTenantIdProvider());

            // Create set for holding registration of customized tenants
            ICustomizedTenantsSet customizedTenants = new CustomizedTenantsSet();
            var customizedTenantsDictionary = new Dictionary<string, IConfigurationSection>();

            // Load customized tenant configuration
            var customizedTenantsSection = config.GetSection("CustomizedTenants");
            if (customizedTenantsSection.Exists())
            {
                foreach (var tenant in customizedTenantsSection.GetChildren().ToList())
                {
                    // Customized tenants must have their properties ALWAYS be fully customized, so for non-customized configuration load the default (unnamed) configuration
                    var tenantId = tenant.Key;
                    customizedTenants.AddTenantId(tenantId);
                    customizedTenantsDictionary.Add(tenantId, tenant);
                }
            }

            // Register configuration models
            var modelTypeInstances = new Dictionary<Type, TwyggerConfig>();
            foreach (var modelType in configModels)
            {
                if (!modelType.IsSubclassOf(typeof(TwyggerConfig)))
                    throw new ArgumentException($"Twygger ConfigModel {modelType.Name} must inherit from {nameof(TwyggerConfig)}");

                var modelTypeInstance = (TwyggerConfig)Activator.CreateInstance(modelType);
                modelTypeInstances.Add(modelType, modelTypeInstance);

                // Configure for customized tenants
                foreach (KeyValuePair<string,IConfigurationSection> tenant in customizedTenantsDictionary)
                {
                    var tenantSpecificModelSection = tenant.Value.GetSection(modelTypeInstance.SectionName);
                    if (tenantSpecificModelSection.Exists())
                        modelTypeInstance.Configure(services, tenant.Key, tenantSpecificModelSection.Exists() ? tenant.Value : config);
                    //services.Configure<DbConfig>(tenantId, dbConfigSection.Exists() ? dbConfigSection : config.GetSection("DbConfig")); // When tenant has no customized section, then add the default section
                }

                // Register the DI retrieval
                modelTypeInstance.Configure(services, config); // Default (unnamed) for non-customized tenants
                modelTypeInstance.Register(services);
            }

            services.AddSingleton<ICustomizedTenantsSet>(customizedTenants);

            return services;
        }
    }
}
