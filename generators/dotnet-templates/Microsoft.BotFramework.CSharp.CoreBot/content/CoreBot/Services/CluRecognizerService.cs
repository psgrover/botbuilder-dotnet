using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CoreBot.Services;

public class CluRecognizerService
{
    private readonly TextAnalyticsClient _client;
    private readonly string _projectName;
    private readonly string _deploymentName;

    public CluRecognizerService(IConfiguration configuration)
    {
        var endpoint = configuration["CluEndpoint"];
        var apiKey = configuration["CluApiKey"];
        _projectName = configuration["CluProjectName"];
        _deploymentName = configuration["CluDeploymentName"];
        _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<ConversationAnalysisResult> RecognizeAsync(string utterance)
    {
        var options = new AnalyzeConversationOptions
        {
            ProjectName = _projectName,
            DeploymentName = _deploymentName
        };
        var response = await _client.AnalyzeConversationAsync(utterance, options);
        return response.Value;
    }
}

public class ConversationAnalysisResult
{
    public string TopIntent { get; }
    public IDictionary<string, string> Entities { get; }

    public ConversationAnalysisResult(AnalyzeConversationResult result)
    {
        TopIntent = result.Prediction.TopIntent;
        Entities = new Dictionary<string, string>();
        foreach (var entity in result.Prediction.Entities)
        {
            Entities[entity.Category] = entity.Text;
        }
    }
}
