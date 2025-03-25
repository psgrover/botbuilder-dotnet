using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

namespace CoreBot.Services;

/// <summary>
/// Service for interacting with the OpenAI GPT-4 API.
/// </summary>
public class OpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _companyName;

    public OpenAIService()
    {
        // Parameterless constructor
    }

    public OpenAIService(IConfiguration configuration)
    {
        var openApiEndpoint = configuration["OpenAIEndpoint"].ToString();
        var apiKey = configuration["OpenAIApiKey"];
        _companyName = configuration["CompanyName"] ?? "our company";
        _client = new OpenAIClient(new Uri(openApiEndpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<string> GenerateResponseAsync(string prompt, string conversationContext)
    {
        var options = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4",
            Messages =
            {
                new ChatRequestSystemMessage($"You are TriageBot, a sales qualification bot for {_companyName}. Respond naturally and adapt to the prospect's context."),
                new ChatRequestUserMessage($"{conversationContext}\n\nProspect: {prompt}")
            },
            MaxTokens = 150,
            Temperature = 0.7f
        };

        var response = await _client.GetChatCompletionsAsync(options);
        return response.Value.Choices[0].Message.Content.Trim();
    }
}
