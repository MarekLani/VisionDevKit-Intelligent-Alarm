using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlarmBackendAPI
{
    public class CosmosHelper
    {

        Database database;
        Container container;

        public CosmosHelper(Database db, Container container)
        {
            this.database = db;
            this.container = container;
        }

        public async Task<List<Entry>> QueryItemsAsync(string query)
        {
            var sqlQueryText = query;

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Entry> queryResultSetIterator = this.container.GetItemQueryIterator<Entry>(queryDefinition);

            List<Entry> entries = new List<Entry>();
            try
            {
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<Entry> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (Entry entry in currentResultSet)
                    {
                        entries.Add(entry);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return entries;
        }

        public class Entry
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string Entrants { get; set; }
            public string Location { get; set; } = "area1";
            public string ImageName { get; set; }

            public Entry(string id, DateTime timeStamp, string entrants, string imageName)
            {
                this.Timestamp = timeStamp;
                this.Entrants = entrants;
                this.Id = id;
                this.ImageName = imageName;
            }
        }
    }
}
