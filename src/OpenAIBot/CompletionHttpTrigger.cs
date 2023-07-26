using System.Net;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace OpenAIBot
{
    public class CompletionHttpTrigger
    {
        private readonly ILogger _logger;

        public CompletionHttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CompletionHttpTrigger>();
        }

        [Function("CompletionHttpTrigger")]

        [OpenApiOperation(operationId: nameof(CompletionHttpTrigger.Run), tags: new[] { "name" })]
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Required = true, Description = "The request body")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]

       public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "completions")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var prompt = req.ReadAsString();

            var endpoint = new Uri(Environment.GetEnvironmentVariable("AOAI_Endpoint"));
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AOAI_ApiKey"));
            var client = new OpenAIClient(endpoint, credential);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are a helpful assistant. You are very good at summarizing the given text into 2-3 bullet points."),
                    new ChatMessage(ChatRole.User, prompt)
                },
                MaxTokens = 800,
                Temperature = 0.7f,
            };

            var deploymentId = Environment.GetEnvironmentVariable("AOAI_DeploymentId");
            var result = await client.GetChatCompletionsAsync(deploymentId, chatCompletionsOptions);
            var message = result.Value.Choices[0].Message.Content;

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(message);

            return response;
        }
    }
}
