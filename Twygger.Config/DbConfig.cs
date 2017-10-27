using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Twygger.Config
{
    public class DbConfig : TwyggerConfig
    {
        public DbConfig()
            : base("DbConfig",
                  (services, name, config) => Configure<DbConfig>(services, name, config, "DbConfig"),
                  (services) => RegisterOptionSnapshot<DbConfig>(services))
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
