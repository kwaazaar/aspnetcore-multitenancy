using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twygger.Config;

namespace MultiTenantWeb
{
    public class CustomConfig : TwyggerConfig
    {
        private const string SECTION_NAME = "CustomConfig";

        public CustomConfig()
            : base(SECTION_NAME,
                  (services, tenantName, config) => ConfigureModel<CustomConfig>(services, tenantName, config, SECTION_NAME),
                  (services) => RegisterModel<CustomConfig>(services))
        {
        }

        public string CustomProp { get; set; }

        public override void SetTenantId(string tenantId)
        {
            CustomProp = CustomProp?.Replace("%tenantid%", tenantId);
        }
    }
}
