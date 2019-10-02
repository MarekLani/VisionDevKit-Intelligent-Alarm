// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared;

namespace VisionAlarmBot
{
    public class VisionAlarmBot : ActivityHandler
    {
        private ILogger<VisionAlarmBot> _logger;
        private IBotServices _botServices;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;

        public VisionAlarmBot(IBotServices botServices, ILogger<VisionAlarmBot> logger, ConcurrentDictionary<string, ConversationReference> conversationReferences, IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _botServices = botServices;
            _conversationReferences = conversationReferences;
            _configuration = configuration;
            _clientFactory = clientFactory;
        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);
            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(turnContext, cancellationToken);

            // Top intent tell us which cognitive service to use.
            var topIntent = recognizerResult.GetTopScoringIntent();

            // Next, we call the dispatcher with the top intent.
            await DispatchToTopIntentAsync(turnContext, topIntent.intent, recognizerResult, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            const string WelcomeText = "Type your question or alarm intent. I can help you answer question related to MS products or show entries captured by vision dev kit";

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to Vision Alarm Bot bot {member.Name}. {WelcomeText}"), cancellationToken);
                }
            }
        }

        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "l_IntelligenAlarmBot":
                    await ProcessAlarmAsync(turnContext, recognizerResult.Properties["luisResult"] as LuisResult, cancellationToken);
                    break;
                case "q_faq":
                    await ProcessSampleQnAAsync(turnContext, cancellationToken);
                    break;
                default:
                    _logger.LogInformation($"Dispatch unrecognized intent: {intent}.");
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Dispatch unrecognized intent: {intent}."), cancellationToken);
                    break;
            }
        }

        private async Task ProcessAlarmAsync(ITurnContext<IMessageActivity> turnContext, LuisResult luisResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessLAlarmAsync");

            var client = _clientFactory.CreateClient("visionBackend");

            // Retrieve LUIS result for Process Automation.
            var result = luisResult.ConnectedServiceResult;
            var topIntent = result.TopScoringIntent.Intent;

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;


            switch (topIntent)
            {
                case "LastEntry":
                    var entry = JsonConvert.DeserializeObject<Entry>(await client.GetStringAsync("LastEntry"));
                    await DisplayEntry(turnContext, entry.ImageName, "Last Entry", "Last entrants were: " + textInfo.ToTitleCase(String.Join(", ", entry.Entrants.ToArray())), entry.Timestamp, cancellationToken);
                    break;
                case "LastIdentifiedPersonEntered":
                    var entry2 = JsonConvert.DeserializeObject<Entry>(await client.GetStringAsync("LastKnownEntrant"));
                    await DisplayEntry(turnContext, entry2.ImageName, "Last know entrant(s)", "Last known entrants were: " + textInfo.ToTitleCase(String.Join(", ", entry2.Entrants.ToArray())), entry2.Timestamp, cancellationToken);
                    break;
                case "PersonArrived":
                    var entry3 = JsonConvert.DeserializeObject<Entry>(await client.GetStringAsync("GetLastEntryForName/" + result.Entities[0].Entity));
                    await DisplayEntry(turnContext, entry3.ImageName, textInfo.ToTitleCase(result.Entities[0].Entity) + " entered at", " ", entry3.Timestamp, cancellationToken);
                    break;
                case "ShowMeTodaysEntrants":
                    var todaysEntrants = JsonConvert.DeserializeObject<string[]>(await client.GetStringAsync("EntrantsOnDay/" + DateTime.Now.Date.ToString("s")));
                    string namesList = "";
                    foreach (var n in todaysEntrants)
                    {
                        namesList += " " + n;
                    }
                    await turnContext.SendActivityAsync("People who entered today:" + textInfo.ToTitleCase(namesList));
                    break;
            }
        }

        private async Task ProcessSampleQnAAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessSampleQnAAsync");

            var results = await _botServices.SampleQnA.GetAnswersAsync(turnContext);
            if (results.Any())
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer in the Q and A system."), cancellationToken);
            }
        }

        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }

        private async Task DisplayEntry(ITurnContext<IMessageActivity> turnContext, string imageName, string title, string text, DateTime timestamp, CancellationToken cancellationToken)
        {
            var cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(url: _configuration["StorageAccountUrl"] + imageName));

            var heroCard = new HeroCard
            {
                Title = title,
                Subtitle = timestamp.ToString(),
                Text = text,
                Images = cardImages,
            };

            var reply = MessageFactory.Attachment(heroCard.ToAttachment());
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
