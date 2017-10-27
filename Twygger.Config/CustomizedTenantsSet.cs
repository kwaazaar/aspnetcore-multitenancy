using System;
using System.Collections.Generic;
using System.Text;

namespace Twygger.Config
{
    public class CustomizedTenantsSet : ICustomizedTenantsSet
    {
        private HashSet<string> customizedTenants = new HashSet<string>();

        public void AddTenantId(string tenantId)
        {
            customizedTenants.Add(tenantId);
        }

        public bool ContainsTenantId(string tenantId)
        {
            return customizedTenants.Contains(tenantId);
        }
    }
}
