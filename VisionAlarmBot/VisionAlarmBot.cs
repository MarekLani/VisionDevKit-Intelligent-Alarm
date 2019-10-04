// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
            var recognizerResult = await _botServices.LuisRecognizer.RecognizeAsync(turnContext, cancellationToken);

            // Next, we call the dispatcher with the top intent.
            await ProcessAlarmAsync(turnContext, recognizerResult, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            const string WelcomeText = "I am inteligent alarm bot and I can show you Last entry, I can tell you when specific person arrived, who was lat identified person to enter and who entered today.";

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to Vision Alarm Bot bot {member.Name}. {WelcomeText}"), cancellationToken);
                }
            }
        }

        private async Task ProcessAlarmAsync(ITurnContext<IMessageActivity> turnContext, RecognizerResult result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessLAlarmAsync");

            var client = _clientFactory.CreateClient("visionBackend");

            var topIntent = result.GetTopScoringIntent().intent;

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
                    var name = result.Entities["Person"][0].ToString();
                    var entry3 = JsonConvert.DeserializeObject<Entry>(await client.GetStringAsync("GetLastEntryForName/" + name));
                    await DisplayEntry(turnContext, entry3.ImageName, textInfo.ToTitleCase(name) + " entered at", " ", entry3.Timestamp, cancellationToken);
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
                default:
                    await turnContext.SendActivityAsync("Sorry your intent was not recognized");
                    break;
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
