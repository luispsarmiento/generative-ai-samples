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

        private string systemMessage = """Eres un asistente de estudio y solo eso. Solo usa la información que te proporcione el usuario. Si te solicitan alguna otra actividad que no sea relacionada con el estudio limita a dar una respuesta.""";
        private string promptText = """Genera preguntas relevantes de exámen tipo test (con opción multiple) con nivel alto de resolución (implica pensamiento análitico y prueba de comprensión de conceptos teoricos) del tema proporcionado entre las etiquetas <input></input>. <input>{0}</input> Devuelve el resultado en un formaton JSON mimificado (limpio sin formato de presentación como Markdown o cualquier otro) como el que se muestra a continuación: [{"Question_text":"¿Cuál es la capital de Francia?","Answer_type":"Multiple choice","Correct_option":"C","Points":"1","Option_1":"Berlin","Option_2":"London","Option_3":"Paris","Option_4":"Madrid"}]. No generes preguntas que no se basen en el tema proporcionado.""";

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

            string? information = await GetInputInformation(req);
            
            if (information is null)
            {
                // Return a 400 bad request  result to the client
                return new BadRequestResult();
            }
            
            string result = GenerateTest(information);
            List<TestGeneratorModelGet> jsonResult = JsonConvert.DeserializeObject<List<TestGeneratorModelGet>>(result);

            return new JsonResult(jsonResult);
        }

        private string GenerateTest(string inputInformation){
            string inputText = promptText.Replace("{0}", inputInformation);

            try{
                OpenAIClient client = new OpenAIClient(new Uri(oaiEndpoint), new AzureKeyCredential(oaiKey));
            
                ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemMessage),
                        new ChatRequestUserMessage(inputText),
                    },
                    Temperature = (float)0.15,
                    MaxTokens = 800,
                    NucleusSamplingFactor = (float)0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
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

    public class TestGeneratorModelGet
    {
        [JsonProperty("Question_text")]
        public string QuestionText { get; set; }

        [JsonProperty("Answer_type")]
        public string AnswerType { get; set; }

        [JsonProperty("Correct_option")]
        public string CorrectOption { get; set; }
        [JsonProperty("Points")]
        public string Points { get; set; }

        [JsonProperty("Option_1")]
        public string Option1 { get; set; }

        [JsonProperty("Option_2")]
        public string Option2 { get; set; }

        [JsonProperty("Option_3")]
        public string Option3 { get; set; }

        [JsonProperty("Option_4")]
        public string Option4 { get; set; }
    }
}
