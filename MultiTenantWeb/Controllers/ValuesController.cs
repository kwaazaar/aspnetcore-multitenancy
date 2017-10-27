using Microsoft.AspNetCore.Mvc;
using Twygger.Config;

namespace MultiTenantWeb.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private DbConfig _dbConfig;
        private CustomConfig _customConfig;

        public ValuesController(DbConfig dbConfig, CustomConfig customConfig)
        {
            _dbConfig = dbConfig;
            _customConfig = customConfig;
        }

        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                DbConfig = _dbConfig,
                CustomConfig = _customConfig
            });
        }
    }
}
