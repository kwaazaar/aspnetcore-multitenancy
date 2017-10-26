using Microsoft.AspNetCore.Mvc;

namespace MultiTenantWeb.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private DbConfig _dbConfig;

        public ValuesController(DbConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        // GET api/values
        [HttpGet]
        public DbConfig Get()
        {
            return _dbConfig;
        }
    }
}
