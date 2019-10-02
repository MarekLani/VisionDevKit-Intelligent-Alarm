using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Shared;

namespace CaptureProcessor
{
    public  class BackendAPI
    {
        [FunctionName("LastEntry")]
        public IActionResult Run(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "LastEntry")] HttpRequest req,
            [CosmosDB("%CosmosDB%", "%CosmosCollection%",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT TOP 1 *  FROM c ORDER BY c.Timestamp DESC")]
                IEnumerable<Entry> entries,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a GetLastEntry request.");

            return (entries.Count() > 0)
                ? (ActionResult)new OkObjectResult(entries.First())
                : new BadRequestObjectResult("Couldn't get last entry");
        }

        [FunctionName("GetLastEntryForName")]
        public IActionResult GetLastEntryForName(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetLastEntryForName/{name}")] HttpRequest req,
            [CosmosDB("%CosmosDB%", "%CosmosCollection%",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT VALUE c FROM c JOIN s in c.Entrants WHERE CONTAINS(s, {name}) ORDER BY c.Timestamp DESC")]
                IEnumerable<Entry> entries,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a GetLastEntryForName request.");

            return (entries.Count() > 0)
                ? (ActionResult)new OkObjectResult(entries.First())
                : new BadRequestObjectResult("Couldn't get entries for provided name. Please pass existing name in url");
        }

        [FunctionName("LastKnownEntrant")]
        public IActionResult LastKnownEntrant(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = "LastKnownEntrant")] HttpRequest req,
          [CosmosDB("%CosmosDB%", "%CosmosCollection%",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT TOP 1 *  FROM c WHERE c.Entrants != '' ORDER BY c.Timestamp DESC")]
                IEnumerable<Entry> entries,
         ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a LastKnownEntrant request.");

            return (entries.Count() > 0)
                ? (ActionResult)new OkObjectResult(entries.First())
                : new BadRequestObjectResult("Couldn't get Last Known Entrant");
        }

        [FunctionName("EntrantsOnDay")]
        public IActionResult EntrantsOnDay(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "EntrantsOnDay/{date}")] HttpRequest req,
         [CosmosDB("%CosmosDB%", "%CosmosCollection%",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT *  FROM c WHERE c.Entrants != '' AND c.Timestamp > {date}")]
                IEnumerable<Entry> entries,
        ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a EntrantsOnDay request.");


            List<string> entrants = new List<string>();
            foreach (var e in entries)
            {
                foreach (var n in e.Entrants)
                {
                    if (n != "" && !entrants.Contains(n))
                        entrants.Add(n);
                }
            }

            return (ActionResult)new OkObjectResult(entrants);  
        }
    }
}
