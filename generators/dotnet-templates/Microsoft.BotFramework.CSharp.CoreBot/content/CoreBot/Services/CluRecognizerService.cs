using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CoreBot.Services;

public class CluRecognizerService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _projectName;
    private readonly string _deploymentName;

    public CluRecognizerService()
    {
        // Parameterless constructor
    }

    public CluRecognizerService(IConfiguration configuration, HttpClient httpClient = null)
    {
        _endpoint = configuration["CluEndpoint"];
        _apiKey = configuration["CluApiKey"];
        _projectName = configuration["CluProjectName"];
        _deploymentName = configuration["CluDeploymentName"];
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
    }

    public async Task<CluResult> RecognizeAsync(string utterance)
    {
        if (string.IsNullOrEmpty(utterance))
        {
            throw new ArgumentNullException(nameof(utterance));
        }

        var url = $"{_endpoint}/language/:analyze-conversations?api-version=2023-04-01";
        var payload = new
        {
            kind = "Conversation",
            analysisInput = new
            {
                conversationItem = new
                {
                    id = "1",
                    text = utterance,
                    modality = "text",
                    language = "en",
                    participantId = "user1"
                }
            },
            parameters = new
            {
                projectName = _projectName,
                deploymentName = _deploymentName,
                stringIndexType = "TextElement_V8"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CluResponse>(jsonResponse);

        return new CluResult
        {
            TopIntent = result?.Result?.Prediction?.TopIntent,
            Entities = ExtractEntities(result?.Result?.Prediction?.Entities)
        };
    }

    private IDictionary<string, string> ExtractEntities(IList<CluEntity> entities)
    {
        var entityDict = new Dictionary<string, string>();
        if (entities != null)
        {
            foreach (var entity in entities)
            {
                if (!entityDict.ContainsKey(entity.Category))
                {
                    entityDict[entity.Category] = entity.Text;
                }
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

public class CluResponse
{
    public CluResultDetail Result { get; set; }
}

public class CluResultDetail
{
    public CluPrediction Prediction { get; set; }
}

public class CluPrediction
{
    public string TopIntent { get; set; }

    public IList<CluEntity> Entities { get; set; }
}

public class CluEntity
{
    public string Category { get; set; }

    public string Text { get; set; }
}
