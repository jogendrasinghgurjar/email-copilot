using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Configuration;

namespace EmailCopilotNew
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("Function1")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "EmailCopilot")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            //IDictionary<string, string> queryParams = req.GetQueryParameterDictionary().ToDictionary(param => param.Key, param => param.Value);
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var input = query["inputText"];
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            var summary = await SementicKernelForEmailCopilot(input);

            response.WriteString(summary.ToString());

            return response;
        }

        private static async Task<Microsoft.SemanticKernel.Orchestration.SKContext> SementicKernelForEmailCopilot(string input)
        {
            //Semantic kernel implementation
            IKernel kernel = Microsoft.SemanticKernel.Kernel.Builder.Build();
            // Grab the locally stored credentials from the settings.json file. 
            // Name the service as "davinci" — assuming that you're using one of the davinci completion models. 
            var (useAzureOpenAI, model, azureEndpoint, apiKey, orgId) = Settings.LoadFromFile();

            if (useAzureOpenAI)
                kernel.Config.AddAzureOpenAITextCompletion("davinci", model, azureEndpoint, apiKey);
            else
                kernel.Config.AddOpenAITextCompletion("davinci", model, apiKey, orgId);

            string mySemanticFunctionInline = """ 
                    {{$input}} 
                    Summarize the content above in less than 250 characters.
                """;

            var promptConfig = new PromptTemplateConfig
            {
                Completion =
                {
                    MaxTokens = 1000, Temperature = 0.2, TopP = 0.5,
                }
            };

            var promptTemplate = new PromptTemplate(
                mySemanticFunctionInline, promptConfig, kernel
            );

            var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);

            var summaryFunction = kernel.RegisterSemanticFunction("MySkill", "Summary", functionConfig);

            Console.WriteLine("A semantic function has been registered.");
            // Text source: https://www.microsoft.com/en-us/worklab/kevin-scott-on-5-ways-generative-ai-will-transform-work-in-2023

            var summary = await kernel.RunAsync(input, summaryFunction);


            Console.WriteLine(summary);
            return summary;
        }
    }
}
