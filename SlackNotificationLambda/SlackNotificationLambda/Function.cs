using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using SlackNotificationLambda.Slack;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SlackNotificationLambda
{
    public class Function
    {
        private readonly SlackClient _slackClient;

        public Function()
        {
            var url = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL");
            if (string.IsNullOrEmpty(url))
                throw new Exception("Missing 'SLACK_WEBHOOK_URL' configuration variable.");
            _slackClient = new SlackClient(url);
        }

        public async Task FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            var log = context.Logger;

            foreach (var record in snsEvent.Records)
            {
                var snsRecord = record.Sns;

                var response = await _slackClient.SendMessageAsync(snsRecord.Message);
                if (!response.IsSuccessStatusCode)
                {
                    log.LogLine($"ERROR [{record.EventSource} {snsRecord.Timestamp}] " +
                                $"SnsMessage = {snsRecord.Message} " +
                                $"HttpStatusCode = {response.StatusCode} " +
                                $"HttpResponse = {response.Content.ReadAsStringAsync().Result}");
                }
            }
        }
    }
}
