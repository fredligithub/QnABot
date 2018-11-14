using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Newtonsoft.Json;

namespace QnABot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        // QnA Maker global settings
        static string qnamaker_en_endpointKey = ConfigurationManager.AppSettings["QnAAuthKey"];
        static string en_kbID = ConfigurationManager.AppSettings["QnAKnowledgebaseId"];
        static string qnamaker_en_endpointHostName = ConfigurationManager.AppSettings["QnAEndpointHostName"];

        static string qnamaker_zn_endpointKey = ConfigurationManager.AppSettings["QnAAuthKeyZN"];
        static string zn_kbID = ConfigurationManager.AppSettings["QnAKnowledgebaseIdZN"];
        static string qnamaker_zn_endpointHostName = ConfigurationManager.AppSettings["QnAEndpointHostNameZN"];

        // Instantiate the knowledge bases
        public QnAMakerService enQnAService = new QnAMakerService(qnamaker_en_endpointHostName, en_kbID, qnamaker_en_endpointKey);
        public QnAMakerService znQnAService = new QnAMakerService(qnamaker_zn_endpointHostName, zn_kbID, qnamaker_zn_endpointKey);

        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"],
            ConfigurationManager.AppSettings["LuisAPIKey"],
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "Greeting" with the name of your newly created intent in the following handler
        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            // await this.ShowLuisResult(context, result);

            // Ask the en knowledge base
            var qnaMakerAnswer = await enQnAService.GetAnswer(result.Query);
            await context.PostAsync($"{qnaMakerAnswer}");

            // Formflow
            var sampleForm = FormDialog.FromForm(Order.BuildForm, FormOptions.PromptInStart);
            context.Call(sampleForm, OrderSubmitted);
        }

        private async Task OrderSubmitted(IDialogContext context, IAwaitable<Order> result)
        {
            context.Wait(MessageReceived);
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        private async Task ShowLuisResult(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"You have reached {result.Intents[0].Intent}. You said: {result.Query}");
            context.Wait(MessageReceived);
        }
    }

    public class Metadata
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Answer
    {
        public IList<string> questions { get; set; }
        public string answer { get; set; }
        public double score { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public IList<object> keywords { get; set; }
        public IList<Metadata> metadata { get; set; }
    }

    public class QnAAnswer
    {
        public IList<Answer> answers { get; set; }
    }

    [Serializable]
    public class QnAMakerService
    {
        private string qnaServiceHostName;
        private string knowledgeBaseId;
        private string endpointKey;

        public QnAMakerService(string hostName, string kbId, string endpointkey)
        {
            qnaServiceHostName = hostName;
            knowledgeBaseId = kbId;
            endpointKey = endpointkey;

        }
        async Task<string> Post(string uri, string body)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", "EndpointKey " + endpointKey);

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<string> GetAnswer(string question)
        {
            string uri = qnaServiceHostName + "/knowledgebases/" + knowledgeBaseId + "/generateAnswer";
            string questionJSON = @"{'question': '" + question + "'}";

            var response = await Post(uri, questionJSON);

            var answers = JsonConvert.DeserializeObject<QnAAnswer>(response);
            if (answers.answers.Count > 0)
            {
                return answers.answers[0].answer;
            }
            else
            {
                return "No good match found.";
            }
        }
    }
}