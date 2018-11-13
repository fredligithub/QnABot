using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Microsoft.Rest;
using QnABot;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.QnABot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private const string SouthCentralUsEndpoint = "https://southcentralus.api.cognitive.microsoft.com";

        public async Task StartAsync(IDialogContext context)
        {
            /* Wait until the first message is received from the conversation and call MessageReceviedAsync 
            *  to process that message. */
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            /* When MessageReceivedAsync is called, it's passed an IAwaitable<IMessageActivity>. To get the message,
             *  await the result. */
            var message = await result as Activity;

            if (message.Attachments.Count > 0)
            {
                // Create a prediction endpoint, passing in obtained prediction key
                CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
                {
                    //Prediction Key from custom vision web site
                    ApiKey = "6c3cfebc453243f987a3966b781f6faf",
                    Endpoint = SouthCentralUsEndpoint
                };

                /* Handle the attachment file from user */
                foreach (var file in message.Attachments)
                {
                    byte[] attachmentBytes = new System.Net.WebClient().DownloadData(file.ContentUrl);
                    MemoryStream attachmentMemoryStream = new MemoryStream(attachmentBytes);
                    //Id as below is the project ID from custom vision web site
                    var predictImgResult = endpoint.PredictImage(Guid.Parse("a4fe9af2-e1b9-45d9-84b9-0abc141f32e2"), attachmentMemoryStream);

                    string replyMsg = string.Empty;
                    foreach (var c in predictImgResult.Predictions)
                    {
                        replyMsg += c.Probability.ToString("0.00") + " could be " + c.TagName + Environment.NewLine;
                    }

                    await context.PostAsync("File received!And the prediction result is: " + replyMsg);
                }
            }
            else
            {
                /* When message return, call Text Analutics service to identify language type */
                ITextAnalyticsClient textanalyticsClient = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
                {
                    Endpoint = ConfigurationManager.AppSettings["TextAnalyticsEndPoint"]
                };

                var languageDetectResult = textanalyticsClient.DetectLanguageAsync(new BatchInput(
                    new List<Input>()
                    {
                    new Input("id", message.Text)
                    })).Result;


                if (languageDetectResult.Documents != null && languageDetectResult.Documents.Count > 0)
                {
                    //ch-ZN support
                    if (languageDetectResult.Documents[0].DetectedLanguages.Count(c => c.Name.Equals("Chinese_Simplified")) > 0)
                    {
                        await context.Forward(new BasicQnAMakerZNDialog(), AfterAnswerAsync, message, CancellationToken.None);
                    }
                    //en-UK support
                    else
                    {
                        await context.Forward(new BasicQnAMakerDialog(), AfterAnswerAsync, message, CancellationToken.None);
                        if (message.Text.ToLower().Contains("mail") || message.Text.ToLower().Contains("manager"))
                        {
                            Mail.SendMail();
                        }
                    }
                }
                //Default support language is en-UK also
                else
                {
                    await context.Forward(new BasicQnAMakerDialog(), AfterAnswerAsync, message, CancellationToken.None);
                }
            }
        }

        private async Task AfterAnswerAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            // wait for the next user message
            //context.Wait(MessageReceivedAsync);

            var sampleForm = FormDialog.FromForm(Order.BuildForm, FormOptions.PromptInStart);
            context.Call(sampleForm, OrderSubmitted);
        }

        private async Task OrderSubmitted(IDialogContext context, IAwaitable<Order> result)
        {
            context.Wait(MessageReceivedAsync);
        }


        public static string GetSetting(string key)
        {
            var value = Utils.GetAppSetting(key);
            if (String.IsNullOrEmpty(value) && key == "QnAAuthKey")
            {
                value = Utils.GetAppSetting("QnASubscriptionKey"); // QnASubscriptionKey for backward compatibility with QnAMaker (Preview)
            }
            return value;
        }
    }

    // Dialog for QnAMaker Preview service
    [Serializable]
    public class BasicQnAMakerPreviewDialog : QnAMakerDialog
    {
        static readonly string qnaAuthKey = ConfigurationManager.AppSettings["QnAAuthKey"];
        static readonly string qnaKBId = ConfigurationManager.AppSettings["QnAKnowledgebaseId"];
        static readonly string endpointHostName = ConfigurationManager.AppSettings["QnAEndpointHostName"];

        // Go to https://qnamaker.ai and feed data, train & publish your QnA Knowledgebase.
        // Parameters to QnAMakerService are:
        // Required: subscriptionKey, knowledgebaseId, 
        // Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]
        public BasicQnAMakerPreviewDialog() : base(new QnAMakerService(
            new QnAMakerAttribute(qnaAuthKey, qnaKBId, "No good match in FAQ!", 0.5, 1, endpointHostName)))
        { }
    }

    // Dialog for QnAMaker GA service
    [Serializable]
    public class BasicQnAMakerDialog : QnAMakerDialog
    {
        static readonly string qnaAuthKey = ConfigurationManager.AppSettings["QnAAuthKey"];
        static readonly string qnaKBId = ConfigurationManager.AppSettings["QnAKnowledgebaseId"];
        static readonly string endpointHostName = ConfigurationManager.AppSettings["QnAEndpointHostName"];

        // Go to https://qnamaker.ai and feed data, train & publish your QnA Knowledgebase.
        // Parameters to QnAMakerService are:
        // Required: qnaAuthKey, knowledgebaseId, endpointHostName
        // Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]
        public BasicQnAMakerDialog() : base(new QnAMakerService(
            new QnAMakerAttribute(qnaAuthKey, qnaKBId, "No good match in FAQ.", 0.01, 1, endpointHostName)))
        { }
    }

    // Dialog for QnAMaker ZN service
    [Serializable]
    public class BasicQnAMakerZNDialog : QnAMakerDialog
    {
        static readonly string qnaAuthKeyZN = ConfigurationManager.AppSettings["QnAAuthKeyZN"];
        static readonly string qnaKBIdZN = ConfigurationManager.AppSettings["QnAKnowledgebaseIdZN"];
        static readonly string endpointHostNameZN = ConfigurationManager.AppSettings["QnAEndpointHostNameZN"];

        public BasicQnAMakerZNDialog() : base(new QnAMakerService(
            new QnAMakerAttribute(qnaAuthKeyZN, qnaKBIdZN, "没有合适的答案.", 0.01, 1, endpointHostNameZN)))
        { }
    }

    public class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string subscriptionKey = ConfigurationManager.AppSettings["TextAnalyticsKey"];
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}