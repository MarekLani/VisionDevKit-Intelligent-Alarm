using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace AlarmBackendAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class EntriesController : Controller
    {

        CosmosClient cosmosClient;
        CosmosHelper cosmosHelper;

        public EntriesController(CosmosHelper ch)
        {
            this.cosmosHelper = ch;
        }

        [HttpGet]
        [Route("GetLastEntryForName")]
        public async Task<ActionResult> GetLastEntryForName([FromQuery]string name)
        {
            return Ok(await cosmosHelper.QueryItemsAsync($"SELECT TOP 1 *  FROM c WHERE CONTAINS(c.Entrants, '{name}') ORDER BY c.Timestamp DESC"));
        }
    }
}