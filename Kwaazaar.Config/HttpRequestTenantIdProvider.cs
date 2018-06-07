using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Kwaazaar.Config
{
    /// <summary>
    /// Default TenantId provider, based on HTTP-header in http request
    /// </summary>
    public class HttpRequestTenantIdProvider : ITenantIdProvider
    {
        private readonly string TenantHeaderName;

        public HttpRequestTenantIdProvider()
            : this ("X-TenantId")
        {
        }

        public HttpRequestTenantIdProvider(string tenantHeaderName)
        {
            if (String.IsNullOrWhiteSpace(tenantHeaderName)) throw new ArgumentNullException(nameof(tenantHeaderName));
            TenantHeaderName = tenantHeaderName;
        }

        public string GetTenantId()
        {
            var ctx = new HttpContextAccessor().HttpContext;
            return ctx?.Request?.Headers.SingleOrDefault(h => h.Key == TenantHeaderName).Value.FirstOrDefault() ?? string.Empty;
        }
    }
}
