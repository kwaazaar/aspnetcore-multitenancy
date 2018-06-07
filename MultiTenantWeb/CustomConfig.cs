using Kwaazaar.Config;

namespace MultiTenantWeb
{
    public class CustomConfig : ConfigModel
    {
        private const string SECTION_NAME = "CustomConfig";

        public CustomConfig()
            : base(SECTION_NAME,
                  (services, tenantName, config) => ConfigureModel<CustomConfig>(services, tenantName, config, SECTION_NAME),
                  RegisterModel<CustomConfig>)
        {
        }

        public string CustomProp { get; set; }
        public string CustomProp2 { get; set; }

        public override void SetTenantId(string tenantId)
        {
            CustomProp = CustomProp?.Replace("%tenantid%", tenantId);
            CustomProp2 = CustomProp2?.Replace("%tenantid%", tenantId);
        }
    }
}
