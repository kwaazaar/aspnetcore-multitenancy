using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Twygger.Config
{
    public abstract class TwyggerConfig
    {
        public readonly Action<IServiceCollection, string, IConfiguration> _configure;
        public readonly Action<IServiceCollection> _registerAction;

        private readonly string _sectionName;
        public string SectionName => _sectionName;

        public TwyggerConfig(string sectionName,
            Action<IServiceCollection, string, IConfiguration> configureAction,
            Action<IServiceCollection> registerAction)
        {
            if (configureAction == null) throw new ArgumentNullException(nameof(configureAction));
            _configure = configureAction;
            _registerAction = registerAction;
            _sectionName = sectionName;
        }

        public void Configure(IServiceCollection services, IConfiguration config)
        {
            _configure(services, string.Empty, config);
        }

        public void Register(IServiceCollection services)
        {
            _registerAction(services);
        }

        public void Configure(IServiceCollection services, string name, IConfiguration config)
        {
            _configure(services, name, config);
        }

        protected static void Configure<T>(IServiceCollection services, string name, IConfiguration config, string sectionName)
            where T : class, new()
        {
            services.Configure<T>(name, config.GetSection(sectionName));
        }

        protected static void RegisterOptionSnapshot<T>(IServiceCollection services)
            where T: class, new()
        {
            services.AddScoped<T>(sp =>
            {
                var currentTenantId = sp.GetService<ITenantIdProvider>().GetTenantId();

                var obj = sp.GetService<ICustomizedTenantsSet>().ContainsTenantId(currentTenantId)
                    ? sp.GetService<IOptionsSnapshot<T>>().Get(currentTenantId)
                    : sp.GetService<IOptionsSnapshot<T>>().Value;

                if (obj != null)
                {
                    var tc = obj as TwyggerConfig;
                    if (tc != null) tc.SetTenantId(currentTenantId);
                }
                return obj;
            });
        }

        public virtual void SetTenantId(string tenantId) { }
    }
}
