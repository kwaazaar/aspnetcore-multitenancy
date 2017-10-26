using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigConsul
{
    public class ServiceConfig
    {
        public string Environment { get; set; }
        public bool IsMultiTenant { get; set; }
    }
}
