using System;
using System.Collections.Generic;
using System.Text;

namespace Twygger.Config
{
    public interface ITenantIdProvider
    {
        string GetTenantId();
    }
}
