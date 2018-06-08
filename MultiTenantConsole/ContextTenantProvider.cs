using Kwaazaar.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MultiTenantConsole
{
    internal class ContextTenantProvider : ITenantIdProvider
    {
        private AsyncLocal<string> _tenantId = new AsyncLocal<string>();

        public void SetTenantId(string tenantId)
        {
            _tenantId.Value = tenantId;
        }

        public string GetTenantId()
        {
            return _tenantId.Value;
        }
    }
}
