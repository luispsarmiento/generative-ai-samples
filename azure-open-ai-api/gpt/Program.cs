using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
// Note: The Azure OpenAI client library for .NET is in preview.
// Install the .NET library via NuGet: dotnet add package Azure.AI.OpenAI --prerelease
using Azure;
using Azure.AI.OpenAI;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

string? oaiEndpoint = config["AzureOAIEndpoint"];
string? oaiKey = config["AzureOAIKey"];
string? oaiDeploymentName = config["AzureOAIDeploymentName"];

if(string.IsNullOrEmpty(oaiEndpoint) || string.IsNullOrEmpty(oaiKey) || string.IsNullOrEmpty(oaiDeploymentName) )
{
    Console.WriteLine("Please check your appsettings.json file for missing or incorrect values.");
    return;
}

OpenAIClient client = new OpenAIClient(new Uri(oaiEndpoint), new AzureKeyCredential(oaiKey));
var systemMessage = @"Eres un asistente de estudio y solo eso. Solo usa la información que te proporcione el usuario. Si te solicitan alguna otra actividad que no sea relacionada con el estudio limita a dar una respuesta.";
do{
    Console.WriteLine("Enter your prompt text (or type 'quit' to exit): ");
    string? inputText = Console.ReadLine();
    if (inputText == "quit") break;

    if (inputText == null) {
        Console.WriteLine("Please enter a prompt.");
        continue;
    }
    
    Console.WriteLine("\nSending request for summary to Azure OpenAI endpoint...\n\n");

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
    Console.WriteLine("Response: " + completion + "\n");
}while(true);
