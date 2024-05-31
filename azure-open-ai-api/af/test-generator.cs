using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.AI.OpenAI;
using System.Text.Json;
﻿using Newtonsoft.Json;

namespace MT.Function
{
    public class test_generator
    {
        private readonly ILogger<test_generator> _logger;

        protected readonly string? oaiEndpoint;
        protected readonly string? oaiKey;
        protected readonly string? oaiDeploymentName;

        public test_generator(ILogger<test_generator> logger)
        {
            _logger = logger;

            oaiEndpoint = GetEnvironmentVariable("AzureOAIEndpoint");
            oaiKey = GetEnvironmentVariable("AzureOAIKey");
            oaiDeploymentName = GetEnvironmentVariable("AzureOAIDeploymentName");
        }

        [Function("test_generator")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation(string.Format("oaiEndpoint: {0}", oaiEndpoint));
            if(string.IsNullOrEmpty(oaiEndpoint) || string.IsNullOrEmpty(oaiKey) || string.IsNullOrEmpty(oaiDeploymentName) )
            {
                return new ConflictResult();
            }

            string? information = await GetInputInformation(req);//"La Guerra Fría fue un enfrentamiento político, económico, social, ideológico, militar y propagandístico que tuvo lugar después de la Segunda Guerra Mundial entre dos bloques principales: Occidental (capitalista) y Oriental (comunista). Estos bloques estaban liderados por los Estados Unidos y la Unión Soviética, respectivamente, su inicio se remonta a 1945.";//model.Information;
            
            if (information is null)
            {
                // Return a 400 bad request  result to the client
                return new BadRequestResult();
            }
            
            string result = GenerateTest(information);

            return new JsonResult("{\"Gre\": " + result + "}");
        }

        private string GenerateTest(string inputInformation){
            string systemMessage = @"Eres un asistente de estudio y solo eso. Solo usa la información que te proporcione el usuario. Si te solicitan alguna otra actividad que no sea relacionada con el estudio limita a dar una respuesta.";
            string inputText = inputInformation;

            try{
                OpenAIClient client = new OpenAIClient(new Uri(oaiEndpoint), new AzureKeyCredential(oaiKey));
            
                ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemMessage),
                        new ChatRequestUserMessage(inputText),
                    },
                    Temperature = (float)0.7,
                    MaxTokens = 800,
                    //NucleusSamplingFactor = (float)0.95,
                    FrequencyPenalty = (float)1.5,
                    PresencePenalty = (float)1.5,
                    DeploymentName = oaiDeploymentName
                };

                ChatCompletions response = client.GetChatCompletions(chatCompletionsOptions);

                string completion = response.Choices[0].Message.Content;
                
                return completion;
            }
            catch(Exception ee){
                return "";
            }
        }

        private async Task<string?> GetInputInformation(HttpRequest req){
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TestGeneratorModel data = JsonConvert.DeserializeObject<TestGeneratorModel>(requestBody);

            return data.Information;
        }

        private static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name);
        }
    }

    public class TestGeneratorModel{
        [JsonProperty("information")]
        public string Information {get; set;} = string.Empty;
    }
}
