using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Twygger.Config
{
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
            return (ctx?.Request != null)
                ? ctx.Request.Headers.SingleOrDefault(h => h.Key == TenantHeaderName).Value.FirstOrDefault() ?? string.Empty
                : null;
        }
    }
}
