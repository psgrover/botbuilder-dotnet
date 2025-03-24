using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using CoreBot.Services;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Three: Tell me about your Business" from the triage call.
/// Uses CLU/GPT-4 for hiring type prompt.
/// </summary>
public class BusinessInfoDialog : ComponentDialog
{
        private readonly OpenAIService _openAIService;
        private readonly CluRecognizerService _cluService;

    public BusinessInfoDialog(OpenAIService openAIService, CluRecognizerService cluService) 
    : base(nameof(BusinessInfoDialog))
    {
        _openAIService = openAIService;
        _cluService = cluService;

        AddDialog(new TextPrompt("RolePrompt"));
        AddDialog(new TextPrompt("CompanyPrompt"));
        AddDialog(new NumberPrompt<int>("TeamSizePrompt"));
        AddDialog(new TextPrompt("HiringTypePrompt"));
        AddDialog(new WaterfallDialog("BusinessInfoWaterfall", new WaterfallStep[]
        {
            RoleStepAsync,
            CompanyStepAsync,
            TeamSizeStepAsync,
            HiringTypeStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "BusinessInfoWaterfall";
    }

    private async Task<DialogTurnResult> RoleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.PromptAsync("RolePrompt", new PromptOptions { Prompt = MessageFactory.Text("Tell me about your role within the business. What are your key responsibilities?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> CompanyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        return await stepContext.PromptAsync("CompanyPrompt", new PromptOptions
        {
            Prompt = MessageFactory.Text("What’s the name of your company?")
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> TeamSizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.TempCompany = (string)stepContext.Result;

        return await stepContext.PromptAsync("TeamSizePrompt", new PromptOptions
        {
            Prompt = MessageFactory.Text($"How many employees are currently at {triageSession.TempCompany}?")
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> HiringTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.TempTeamSize = (int)stepContext.Result;

        return await stepContext.PromptAsync("HiringTypePrompt", new PromptOptions
        {
            Prompt = MessageFactory.Text("Are you looking to hire full-time employees, contractors, or both?")
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        var hiringTypeResponse = (string)stepContext.Result;

        var cluResult = await _cluService.RecognizeAsync(hiringTypeResponse);
        triageSession.TempHiringType = cluResult.Entities.TryGetValue("HiringType", out var hiringType) ? hiringType : hiringTypeResponse;

        var context = $"Prospect works at {triageSession.TempCompany} with {triageSession.TempTeamSize} employees and needs {triageSession.TempHiringType}.";
        var gptResponse = await _openAIService.GenerateResponseAsync("Confirm their hiring needs and transition to next topic.", context);
        await stepContext.Context.SendActivityAsync(MessageFactory.Text(gptResponse), cancellationToken);

        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
