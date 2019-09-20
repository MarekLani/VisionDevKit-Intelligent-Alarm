using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace CaptureProcessor
{
    public static class ProcessCapture
    {
        [FunctionName("Function1")]
        public static async Task Run([BlobTrigger("vision/{name}", Connection = "StorageConnectionString")]Stream myBlob, string name,
            [CosmosDB(
                databaseName: "%CosmosDB%",
                collectionName: "%CosmosCollection%",
                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<Entry> document,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            var fc = new FaceClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("FaceApiKey"))) { Endpoint = Environment.GetEnvironmentVariable("FaceApiEndpoint"), };

            //make sure you specify Recognition Model, which was used when creating person in group
            var detectedFaces = await fc.Face.DetectWithStreamAsync(myBlob, recognitionModel: RecognitionModel.Recognition02);
            var groups = await fc.PersonGroup.ListAsync();

            //Get timestamp from image name (6 signs capture prefix, 10 .jpg suffix + 6 capture)
            var timeStampString = name.Substring(8, name.Length - 12);
            var dto = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(timeStampString));
            var timestamp = dto.DateTime;

            string entrants = "";

            var dfIds = detectedFaces.Select(f => f.FaceId.ToGuid()).ToList();

            try
            {
                var identifiedFaces = await fc.Face.IdentifyAsync(dfIds, groups.First().PersonGroupId);
                if (identifiedFaces.Count > 0)
                {
                    foreach (var f in identifiedFaces)
                    {
                        if (f.Candidates.First().Confidence > 0.7)
                        {
                            var pInfo = await fc.PersonGroupPerson.GetAsync(groups.First().PersonGroupId, f.Candidates.First().PersonId);
                            //We enclose name, so we can later filter based on the name if not enclosed contains "Marek" would fit for all Mareks in the entrants list
                            entrants += "|"+pInfo.Name+"|";
                           

                        }
                    }
                }
                var entry = new Entry(Guid.NewGuid().ToString(), timestamp, entrants, name);
                await document.AddAsync(entry);

                if (entrants == "") {
                    //If no known entrant identified, we need to send notification
                    string accountSid = Environment.GetEnvironmentVariable("TwilioAccountSid").ToString();
                    string authToken = Environment.GetEnvironmentVariable("TwilioAuthToken");

                    TwilioClient.Init(accountSid, authToken);

                    var message = MessageResource.Create(
                        body: "Warning, unidentified entrant!",
                        from: new Twilio.Types.PhoneNumber("+12512559108"),
                        to: new Twilio.Types.PhoneNumber("+421902113441")
                    );
                }

                log.LogInformation(identifiedFaces.Count().ToString());
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
            }


        }
    }

    public class Entry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Entrants { get; set; }
        public string Location { get; set; } = "area1";
        public string ImageName {get;set;}

        public Entry(string id, DateTime timeStamp, string entrants, string imageName)
        {
            this.Timestamp = timeStamp;
            this.Entrants = entrants;
            this.Id = id;
            this.ImageName = imageName;
        }
    }

    public static class Extension
    {
        public static Guid ToGuid(this Guid? source)
        {
            return source ?? Guid.Empty;
        }

        // more general implementation 
        public static T ValueOrDefault<T>(this Nullable<T> source) where T : struct
        {
            return source ?? default(T);
        }
    }
}
