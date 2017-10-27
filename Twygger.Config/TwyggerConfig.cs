using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Twygger.Config
{
    /// <summary>
    /// Base-class for configuration models
    /// </summary>
    public abstract class TwyggerConfig
    {
        /// <summary>
        /// Delegate for configuring a model as an Option
        /// </summary>
        /// <param name="services">servicecollection</param>
        /// <param name="tenantName">name of the tenant (empty string for none)</param>
        /// <param name="configRoot">config-object that contains the section</param>
        protected delegate void ConfigureModelDelegate(IServiceCollection services, string tenantName, IConfiguration configRoot);

        /// <summary>
        /// Delegate for registering a model for DI
        /// </summary>
        /// <param name="services">servicecollection</param>
        protected delegate void RegisterModelDelegate(IServiceCollection services);

        // Backing variables
        private readonly ConfigureModelDelegate _configureModelDelegate;
        private readonly RegisterModelDelegate _registerModelDelegate;
        private readonly string _sectionName;

        /// <summary>
        /// SectionName that represents this object in configuration
        /// </summary>
        internal string SectionName => _sectionName;

        /// <summary>
        /// Instantiates a new config model
        /// </summary>
        /// <remarks>The ugly delegates are nessecary because the models' type can only be provided as a type-param, not as a Type-variable.</remarks>
        /// <param name="sectionName">SectionName in the configuration, eg: DbConfig</param>
        /// <param name="configureModelDelegate">Delegate that must invoke TwyggerConfig.ConfigureModel&lt;TModel&gt; method</param>
        /// <param name="registerModelDelegate">Delegate that must invoke TwyggerConfig.RegisterModel&lt;TModel&gt; method</param>
        protected TwyggerConfig(string sectionName, ConfigureModelDelegate configureModelDelegate, RegisterModelDelegate registerModelDelegate)
        {
            if (String.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException(nameof(sectionName));
            if (configureModelDelegate == null) throw new ArgumentNullException(nameof(configureModelDelegate));
            if (registerModelDelegate == null) throw new ArgumentNullException(nameof(registerModelDelegate));

            _sectionName = sectionName;
            _configureModelDelegate = configureModelDelegate;
            _registerModelDelegate = registerModelDelegate;
        }

        /// <summary>
        /// Applies TenantId to this model. A possible implementation could be to replace a tenantid-variable with the actual tenantid.
        /// Has no default implementation.
        /// </summary>
        /// <param name="tenantId">The active tenant, could be empty string</param>
        public virtual void SetTenantId(string tenantId) { }

        internal void RegisterModel(IServiceCollection services)
        {
            _registerModelDelegate(services);
        }

        internal void ConfigureModel(IServiceCollection services, string tenantName, IConfiguration config)
        {
            _configureModelDelegate(services, tenantName ?? Options.DefaultName, config);
        }

        #region Static functions for use in constructor actions
        /// <summary>
        /// Configures a model as an auto-reloadable option (IOptionsSnapshot<typeparamref name="T"/>)
        /// </summary>
        /// <param name="services">servicecollection</param>
        /// <param name="tenantName">name of the tenant (null for none)</param>
        /// <param name="config">configuration that contains the section for the model</param>
        /// <param name="sectionName">sectionname for the section that represents the model</param>
        protected static void ConfigureModel<T>(IServiceCollection services, string tenantName, IConfiguration config, string sectionName)
            where T : class, new()
        {
            services.Configure<T>(tenantName ?? Options.DefaultName, config.GetSection(sectionName));
        }

        /// <summary>
        /// Registers a model for DI
        /// </summary>
        /// <typeparam name="T">Type of the model</typeparam>
        /// <param name="services">IServiceCollection</param>
        protected static void RegisterModel<T>(IServiceCollection services)
            where T: class, new()
        {
            services.AddScoped<T>(sp =>
            {
                var currentTenantId = sp.GetService<ITenantIdProvider>().GetTenantId();

                var obj = sp.GetService<ICustomizedTenantsSet>().ContainsTenantId(currentTenantId)
                    ? sp.GetService<IOptionsSnapshot<T>>().Get(currentTenantId) // IOptionsSnapshot<T> will always return a value (empty instance when there is nothing configured for this tenant)
                    : sp.GetService<IOptionsSnapshot<T>>().Value;

                if (obj != null)
                {
                    var tc = obj as TwyggerConfig;
                    if (tc != null) tc.SetTenantId(currentTenantId);
                }
                return obj;
            });
        }
        #endregion
    }
}
