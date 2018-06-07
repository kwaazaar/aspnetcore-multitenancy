using System;

namespace Kwaazaar.Config
{
    /// <summary>
    /// Basic database configuration model, can be used as an example
    /// </summary>
    public class DbConfig : ConfigModel
    {
        private const string SECTION_NAME = "DbConfig";

        public DbConfig() : base(SECTION_NAME,
                  (services, tenantName, config) => ConfigureModel<DbConfig>(services, tenantName, config, SECTION_NAME),
                  RegisterModel<DbConfig>)
        {
        }

        public override void SetTenantId(string tenantId)
        {
            const string tenantPlaceholder = "%tenantid%";
            Server = Server?.Replace(tenantPlaceholder, tenantId);
            Database = Database?.Replace(tenantPlaceholder, tenantId);
            Username = Username?.Replace(tenantPlaceholder, tenantId);
            Password = Password?.Replace(tenantPlaceholder, tenantId);
        }

        public override void Validate()
        {
            if (Server == null || Database == null || Username == null || Password == null)
                throw new ArgumentException("DbConfig configuration is not valid");
        }

        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
