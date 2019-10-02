using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace VisionAlarmBot.Controllers
{
 
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
        private readonly IConfiguration _configuration;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"];
            _configuration = configuration;

            // If the channel is the Emulator, and authentication is not in use,
            // the AppId will be null.  We generate a random AppId for this case only.
            // This is not required for production, since the AppId will have a value.
            if (string.IsNullOrEmpty(_appId))
            {
                _appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
            }
        }

        public async Task<IActionResult> Post([FromBody] ProactiveMessage message)
        {

            await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, _conversationReferences.Values.Last(), CreateCallback(message), default(CancellationToken));
            // Let the caller know proactive messages have been sent
            return new OkResult();

        }

        private BotCallbackHandler CreateCallback(ProactiveMessage message)
        {
            return async (turnContext, token) =>
            {
                try
                {
                    var cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: _configuration["StorageAccountUrl"] + message.ImageName));

                    var heroCard = new HeroCard
                    {
                        Title = "New Entry!!",
                        Subtitle = message.Text,
                        Images = cardImages,
                    };

                    var reply = MessageFactory.Attachment(heroCard.ToAttachment());
                    await turnContext.SendActivityAsync(reply);
                    // Send the user a proactive confirmation message.
                }
                catch (Exception e)
                {
                    //TODO handle error logging
                    throw e;
                }
            };
        }
    }

    public class ProactiveMessage
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("imageName")]
        public string ImageName { get; set; }
    }
}