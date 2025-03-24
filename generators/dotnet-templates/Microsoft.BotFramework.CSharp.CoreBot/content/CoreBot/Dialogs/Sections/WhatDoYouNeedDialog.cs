using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using CoreBot.Services;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Six: What do you need?" from the triage call.
/// </summary>
public class WhatDoYouNeedDialog : ComponentDialog
{
    private readonly OpenAIService _openAIService;
    private readonly CluRecognizerService _cluService;

    public WhatDoYouNeedDialog(OpenAIService openAIService, CluRecognizerService cluService) 
    : base(nameof(WhatDoYouNeedDialog))
    {
        _openAIService = openAIService;
        _cluService = cluService;

        AddDialog(new TextPrompt("NeedsPrompt"));
        AddDialog(new WaterfallDialog("WhatDoYouNeedWaterfall", new WaterfallStep[]
        {
            NeedsStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "WhatDoYouNeedWaterfall";
    }

    private async Task<DialogTurnResult> NeedsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        var prompt = triageSession.TempHiringType != null
            ? $"What specific {triageSession.TempHiringType} hiring needs do you have at {triageSession.TempCompany}?"
            : "What specific hiring needs do you have at your company?";
        return await stepContext.PromptAsync("NeedsPrompt", new PromptOptions
        {
            Prompt = MessageFactory.Text(prompt)
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        var needsResponse = (string)stepContext.Result;

        var cluResult = await _cluService.RecognizeAsync(needsResponse);
        triageSession.TempDesiredOutcome = needsResponse;

        var context = $"Prospect from {triageSession.TempCompany} needs {triageSession.TempHiringType}: {needsResponse}.";
        var gptResponse = await _openAIService.GenerateResponseAsync("Acknowledge their needs and ask if they’re ready to discuss timing.", context);
        await stepContext.Context.SendActivityAsync(MessageFactory.Text(gptResponse), cancellationToken);

        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
