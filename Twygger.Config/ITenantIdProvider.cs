using System;
using System.Collections.Generic;
using System.Text;

namespace Kwaazaar.Config
{
    /// <summary>
    /// Provides Tenant-Id
    /// </summary>
    public interface ITenantIdProvider
    {
        /// <summary>
        /// Get the TenantId for current context (eg request)
        /// </summary>
        /// <returns>Tenant Id</returns>
        string GetTenantId();
    }
}
