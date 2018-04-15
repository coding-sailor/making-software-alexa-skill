using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlackNotificationLambda.Slack
{
    public class SlackClient
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Uri _url;

        public SlackClient(string url)
        {
            _url = new Uri(url);
        }

        public async Task<HttpResponseMessage> SendMessageAsync(string messageText)
        {
            var message = new SlackMessage
            {
                Text = messageText
            };
            return await SendMessageAsync(message);
        }

        public async Task<HttpResponseMessage> SendMessageAsync(SlackMessage message)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);
            var content = new StringContent(serializedMessage, Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync(_url, content);
        }
    }
}
