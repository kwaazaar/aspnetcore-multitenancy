using Kwaazaar.Config;

namespace MultiTenantWeb
{
    public class CustomConfig : ConfigModel
    {
        public string CustomProp { get; set; }
        public string CustomProp2 { get; set; }

        public override void SetTenantId(string tenantId)
        {
            CustomProp = CustomProp?.Replace("%tenantid%", tenantId);
            CustomProp2 = CustomProp2?.Replace("%tenantid%", tenantId);
        }
    }
}
