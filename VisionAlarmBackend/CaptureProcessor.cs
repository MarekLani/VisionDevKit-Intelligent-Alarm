using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace CaptureProcessor
{
    public class ProcessCapture
    {
        private readonly FaceClient _faceClient;
        private readonly HttpClient _httpClient;
        public ProcessCapture(FaceClient fc, IHttpClientFactory httpClientFactory)
        {
            this._faceClient = fc;
            _httpClient = httpClientFactory.CreateClient();
        }


        [FunctionName("ProcessCapture")]
        public async Task Run([BlobTrigger("vision/{name}", Connection = "StorageConnectionString")]Stream myBlob, string name,
            [CosmosDB(
                databaseName: "%CosmosDB%",
                collectionName: "%CosmosCollection%",
                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<Entry> document, 
            [TwilioSms(
                AccountSidSetting = "TwilioAccountSid",
                AuthTokenSetting = "TwilioAuthToken", 
                From = "%FromNumber%")] IAsyncCollector<CreateMessageOptions> smsOptions,
            ILogger log)
        {
                        
            //We need to obtain linux timestam from image name
            //(8 chars "capture_" prefix, 12 = prefix + ."jpg" suffix)
            var timeStampString = name.Substring(8, name.Length - 12);
            var dto = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(timeStampString));
            var timestamp = dto.DateTime;

            List<string> entrants = new List<string>();

            try
            {
                //Make sure you specify Recognition Model, which was used when creating person in person group
                var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(myBlob, recognitionModel: RecognitionModel.Recognition02);
                var group = (await _faceClient.PersonGroup.ListAsync())
                    .Where(g => g.Name == Environment.GetEnvironmentVariable("PersonGroupName")).FirstOrDefault();

                var dfIds = detectedFaces.Select(f => f.FaceId.ToGuid()).ToList();

                if (dfIds.Count != 0 && group != null)
                {
                    var identifiedFaces = await _faceClient.Face.IdentifyAsync(dfIds, group.PersonGroupId);
                    log.LogInformation(identifiedFaces.Count().ToString());

                    if (identifiedFaces.Count > 0)
                    {
                        foreach (var f in identifiedFaces)
                        {
                            if (f.Candidates.Count > 0 && f.Candidates.First().Confidence > 0.7)
                            {
                                var pInfo = await _faceClient.PersonGroupPerson.GetAsync(group.PersonGroupId, f.Candidates.First().PersonId);
                                //We put it to lower, as LUIS entities are always returned in "ToLower" form
                                entrants.Add(pInfo.Name.ToLower());
                            }
                        }
                    }
                }

                var entry = new Entry(Guid.NewGuid().ToString(), timestamp, entrants, name.ToLower());
                await document.AddAsync(entry);

                if (!entrants.Any()) {
                    //If no known entrant identified, we send sms notification
                    await smsOptions.AddAsync(new CreateMessageOptions(Environment.GetEnvironmentVariable("ToNumber"))
                    {
                        Body = "Warning, unidentified entrant!"
                    });
                }

                //Invoking Bot Proactive message by sending request to proactive endpoint 
                var content = new StringContent($"{{\"imageName\":\"{name}\",\"text\":\"{String.Join(", ", entrants.ToArray())}\"}}", Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(Environment.GetEnvironmentVariable("ProactiveBotEndpoint"),content);

               
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
            }
            
            log.LogInformation("Succesfully processed blob: "+name);
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
