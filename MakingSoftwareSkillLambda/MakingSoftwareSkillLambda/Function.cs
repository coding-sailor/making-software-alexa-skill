using System;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET.Response.Ssml;
using Alexa.NET.Response.Ssml.SoundLibrary;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MakingSoftwareSkillLambda
{
    public class Function
    {
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {

            var log = context.Logger;
            log.LogLine($"Skill request: {JsonConvert.SerializeObject(input)}");

            switch (input.Request)
            {
                case LaunchRequest _:
                    return ResponseBuilder.Ask("Welcome at Making Software.", new Reprompt
                    {
                        OutputSpeech = new PlainTextOutputSpeech
                        {
                            Text = "You can ask me something about Making Software"
                        }
                    });
                case IntentRequest intentRequest:
                    return GetIntentResponse(intentRequest);
                default:
                    return ResponseBuilder.Empty();
            }
        }

        private SkillResponse GetIntentResponse(IntentRequest request)
        {
            string text;
            switch (request.Intent.Name)
            {
                case "Info":
                    text = "Making Software is a forum for technology news, networking " +
                           "and events relevant to the local software development industry in Krakow.";
                    break;
                case "Agenda":
                    text = GetEventText(request.Intent.Slots["time"]?.Value)
                           ?? "I couldn't find anything at that time.";
                    break;
                case "Order":
                    return GetOrderResponse(request);
                case "AMAZON.CancelIntent":
                case "AMAZON.StopIntent":
                    text = "Bye.";
                    break;
                default:
                    text = "Sorry, I couldn't find an answer to your question.";
                    break;
            }

            return ResponseBuilder.Tell(text);
        }

        private SkillResponse GetOrderResponse(IntentRequest request)
        {
            if (request.DialogState != DialogState.Completed)
            {
                return ResponseBuilder.DialogDelegate();
            }
            if (request.Intent.ConfirmationStatus == ConfirmationStatus.Confirmed && int.TryParse(request.Intent.Slots["number"]?.Value, out var number))
            {
                var snsClient = new AmazonSimpleNotificationServiceClient();
                var snsPublishRequest = new PublishRequest
                {
                    TopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC"),
                    Message = $"New beer order. Amount: {number}"
                };
                snsClient.PublishAsync(snsPublishRequest).Wait();

                var plainText = new PlainText($"{number} {(number > 1 ? "beers" : "beer")} ordered.");
                var speech = new Speech(plainText, Foley.GlassesClick02);
                return ResponseBuilder.Tell(speech);
            }

            return ResponseBuilder.Empty();
        }

        private string GetEventText(string timeText)
        {
            if (timeText == null || !TimeSpan.TryParse(timeText.Contains(":") ? timeText : $"{timeText}:00", out var result))
                return null;

            var text = Agenda
                .OrderBy(x => x.time)
                .TakeWhile(x => x.time <= result)
                .Select(x => x.text)
                .LastOrDefault();

            return text != null ? $"There is {text}" : null;
        }

        private static IEnumerable<(TimeSpan time, string text)> Agenda
            => new[]
            {
                (new TimeSpan(17, 30, 0), "a registration, food and drinks"),
                (new TimeSpan(18, 00, 0), "a presentation: Alexa, bring me a beer. Presented by Janusz Mikrut"),
                (new TimeSpan(19, 00, 0), "a break"),
                (new TimeSpan(19, 30, 0), "a presentation: Data streaming with Cosmos DB. Presented by Hovhannes Gulyan"),
                (new TimeSpan(20, 30, 0), "an after-party! Beer, networking, hang out"),
            };
    }
}
