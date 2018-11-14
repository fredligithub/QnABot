using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using QnABot;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Microsoft.Bot.Sample.QnABot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                // detect language type
                ITextAnalyticsClient textanalyticsClient = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
                {
                    Endpoint = ConfigurationManager.AppSettings["TextAnalyticsEndPoint"]
                };

                var languageDetectResult = textanalyticsClient.DetectLanguageAsync(new BatchInput(
                    new List<Input>()
                    {
                    new Input("id", activity.Text)
                    })).Result;

                if (languageDetectResult.Documents != null && languageDetectResult.Documents.Count > 0)
                {
                    //ZN support
                    if (languageDetectResult.Documents[0].DetectedLanguages.Count(c => c.Name.Equals("Chinese_Simplified")) > 0)
                    {
                        await Conversation.SendAsync(activity, () => new BasicLuisDialog());
                    }
                    //EN support
                    if (languageDetectResult.Documents[0].DetectedLanguages.Count(c => c.Name.Equals("English")) > 0)
                    {
                        await Conversation.SendAsync(activity, () => new BasicLuisDialog());
                    }
                }
                else
                {
                    await Conversation.SendAsync(activity, () => new BasicLuisDialog());
                    // default code
                    // await Conversation.SendAsync(activity, () => new RootDialog());
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                //ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                //Activity reply = message.CreateReply("Hello from my simple Bot!");
                //connector.Conversations.ReplyToActivityAsync(reply);

                IConversationUpdateActivity update = message;
                var client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials());
                if (update.MembersAdded != null && update.MembersAdded.Any())
                {
                    foreach (var newMember in update.MembersAdded)
                    {
                        if (newMember.Id != message.Recipient.Id)
                        {
                            var reply = message.CreateReply();
                            reply.Text = $"Welcome to Terminator 2018!";
                            client.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                }

            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}