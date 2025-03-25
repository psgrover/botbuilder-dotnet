using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CoreBot.Models;

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

    public async Task<CluResult> RecognizeAsync(string utterance)
    {
        if (string.IsNullOrEmpty(utterance))
            throw new ArgumentNullException(nameof(utterance));

        /*var options = new AnalyzeConversationOptions
        {
            ProjectName = _projectName,
            DeploymentName = _deploymentName
        };*/

        // var response = await _client.AnalyzeConversationAsync(utterance, options);
        var response = await _client.AnalyzeConversationAsync(utterance);
        var result = response.Value; // AnalyzeConversationResult

        return new CluResult
        {
            TopIntent = result.Prediction.TopIntent,
            Entities = ExtractEntities(result.Prediction.Entities)
        };
    }

    private IDictionary<string, string> ExtractEntities(IReadOnlyList<CategorizedEntity> entities)
    {
        var entityDict = new Dictionary<string, string>();
        foreach (var entity in entities)
        {
            if (!entityDict.ContainsKey(entity.Category))
            {
                entityDict[entity.Category] = entity.Text;
            }
        }
        return entityDict;
    }
}


public class CluResult
{
    public string TopIntent { get; set; }
    public IDictionary<string, string> Entities { get; set; }
}
