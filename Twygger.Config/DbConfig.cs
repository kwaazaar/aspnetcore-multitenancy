namespace Twygger.Config
{
    public class DbConfig : TwyggerConfig
    {
        private const string SECTION_NAME = "DbConfig";

        public DbConfig()
            : base(SECTION_NAME, 
                  (services, tenantName, config) => ConfigureModel<DbConfig>(services, tenantName, config, SECTION_NAME),
                  (services) => RegisterModel<DbConfig>(services))
        {
        }

        public override void SetTenantId(string tenantId)
        {
            Server = Server?.Replace("%tenantid%", tenantId);
            Database = Database?.Replace("%tenantid%", tenantId);
        }

        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
