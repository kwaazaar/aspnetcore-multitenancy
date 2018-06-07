using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Kwaazaar.Config
{
    /// <summary>
    /// Base-class for configuration models
    /// </summary>
    public abstract class ConfigModel
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
        protected ConfigModel(string sectionName, ConfigureModelDelegate configureModelDelegate, RegisterModelDelegate registerModelDelegate)
        {
            if (String.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException(nameof(sectionName));
            _sectionName = sectionName;
            _configureModelDelegate = configureModelDelegate ?? throw new ArgumentNullException(nameof(configureModelDelegate));
            _registerModelDelegate = registerModelDelegate ?? throw new ArgumentNullException(nameof(registerModelDelegate));
        }

        /// <summary>
        /// Applies TenantId to this model. A possible implementation could be to replace a tenantid-variable with the actual tenantid.
        /// Has no default implementation.
        /// </summary>
        /// <param name="tenantId">The active tenant, could be empty string</param>
        public virtual void SetTenantId(string tenantId) { }

        /// <summary>
        /// Validate the model after retrieval
        /// </summary>
        public virtual void Validate() { }

        /// <summary>
        /// Invoke the registerModelDelegate to registers a model for DI. The model is registered as scoped and a custom action retrieves its value using
        /// an IOptionsSnapshot to enable auto-reloading. After loading, SetTenantId is invoked on the model.
        /// </summary>
        /// <typeparam name="T">Type of the model</typeparam>
        /// <param name="services">IServiceCollection</param>
        internal void RegisterModel(IServiceCollection services)
        {
            _registerModelDelegate(services);
        }

        /// <summary>
        /// Invoke the configureModelDelegate to configures a model as IOption<typeparamref name="T"/>
        /// </summary>
        /// <param name="services">servicecollection</param>
        /// <param name="tenantName">name of the tenant (null for none)</param>
        /// <param name="config">configuration that contains the section for the model</param>
        internal void ConfigureModel(IServiceCollection services, string tenantName, IConfiguration config)
        {
            _configureModelDelegate(services, tenantName ?? Options.DefaultName, config);
        }

        #region Static functions for use in constructor actions
        /// <summary>
        /// Configures a model as IOption<typeparamref name="T"/>
        /// </summary>
        /// <param name="services">servicecollection</param>
        /// <param name="tenantName">name of the tenant (null for none)</param>
        /// <param name="config">configuration that contains the section for the model</param>
        /// <param name="sectionName">sectionname for the section that represents the model</param>
        protected static void ConfigureModel<T>(IServiceCollection services, string tenantName, IConfiguration config, string sectionName)
            where T : class, new()
        {
            services.Configure<T>(tenantName ?? Options.DefaultName, config.GetSection(sectionName)); //  ?? typeof(T).GetType().Name
        }

        /// <summary>
        /// Registers a model for DI. The model is registered as scoped and a custom action retrieves its value using
        /// an IOptionsSnapshot to enable auto-reloading. After loading, SetTenantId is invoked on the model.
        /// </summary>
        /// <typeparam name="T">Type of the model</typeparam>
        /// <param name="services">IServiceCollection</param>
        protected static void RegisterModel<T>(IServiceCollection services)
            where T: class, new()
        {
            services.AddScoped<T>(sp => // Scoped lifetime (ASP.Net creates scope for every request)
            {
                var currentTenantId = sp.GetService<ITenantIdProvider>().GetTenantId();

                var obj = sp.GetService<ICustomizedTenantsSet>().ContainsTenantId(currentTenantId)
                    ? sp.GetService<IOptionsSnapshot<T>>().Get(currentTenantId) // IOptionsSnapshot<T> will always return a value (empty instance when there is nothing configured for this tenant)
                    : sp.GetService<IOptionsSnapshot<T>>().Value;

                if (obj != null)
                {
                    if (obj is ConfigModel tc)
                    {
                        tc.Validate();
                        tc.SetTenantId(currentTenantId);
                    }
                }
                return obj;
            });
        }
        #endregion
    }
}
