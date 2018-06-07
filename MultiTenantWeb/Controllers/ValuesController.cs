using Microsoft.AspNetCore.Mvc;
using Kwaazaar.Config;

namespace MultiTenantWeb.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly DbConfig _dbConfig;
        private readonly CustomConfig _customConfig;

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
