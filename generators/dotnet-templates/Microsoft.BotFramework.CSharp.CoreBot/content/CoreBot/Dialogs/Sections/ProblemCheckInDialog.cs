using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Eight: Problem Check-In" from the triage call.
/// </summary>
public class ProblemCheckInDialog : ComponentDialog
{
    public ProblemCheckInDialog() : base(nameof(ProblemCheckInDialog))
    {
        AddDialog(new ConfirmPrompt("SummaryPrompt"));
        AddDialog(new TextPrompt("AdditionsPrompt"));
        AddDialog(new WaterfallDialog("ProblemCheckInWaterfall", new WaterfallStep[]
        {
            SummaryStepAsync,
            AdditionsStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "ProblemCheckInWaterfall";
    }

    private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        var summary = $"So just to check, your objectives are '{triageSession.TempObjectives}', but you feel like '{triageSession.TempKeyChallenges}' and what you want from us is '{triageSession.TempDesiredOutcome}'. Is that right?";
        return await stepContext.PromptAsync("SummaryPrompt", new PromptOptions { Prompt = MessageFactory.Text(summary) }, cancellationToken);
    }

    private async Task<DialogTurnResult> AdditionsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.IsSummaryConfirmed = (bool)stepContext.Result;
        if (triageSession.IsSummaryConfirmed)
        {
            return await stepContext.PromptAsync("AdditionsPrompt", new PromptOptions { Prompt = MessageFactory.Text("Is there anything I’ve missed or you’d like to add?") }, cancellationToken);
        }
        return await stepContext.NextAsync(null, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        if (stepContext.Result != null) // Additions provided
        {
            triageSession.TempKeyChallenges += $"; {(string)stepContext.Result}";
        }
        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
