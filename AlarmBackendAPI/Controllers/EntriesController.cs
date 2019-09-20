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

        [HttpGet]
        [Route("GetLastEntry")]
        public async Task<ActionResult> GetLastEntry()
        {
            return Ok(await cosmosHelper.QueryItemsAsync($"SELECT TOP 1 *  FROM c ORDER BY c.Timestamp DESC"));
        }

        [HttpGet]
        [Route("LastKnownEntrant")]
        public async Task<ActionResult> LastKnownEntrant()
        {
            return Ok(await cosmosHelper.QueryItemsAsync($"SELECT TOP 1 *  FROM c WHERE c.Entrants != '' ORDER BY c.Timestamp DESC"));
        }

        [HttpGet]
        [Route("TodaysEntrants")]
        public async Task<ActionResult> TodaysEntrants()
        {
            var res = await cosmosHelper.QueryItemsAsync($"SELECT *  FROM c WHERE c.Entrants != '' AND c.Timestamp > '{DateTime.Now.Date.ToString("s")}'");
            List<string> entrants = new List<string>();
            foreach(var e in res)
            {
                var names = e.Entrants.Split('|');
                foreach(var n in names)
                {
                    if (n != "" && !entrants.Contains(n))
                        entrants.Add(n);
                }
            }

            return Ok(entrants);
        }
    }
}