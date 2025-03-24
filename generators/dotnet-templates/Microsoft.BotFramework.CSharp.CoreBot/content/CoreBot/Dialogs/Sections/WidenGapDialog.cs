using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Four: Widen the Gap" from the triage call.
/// </summary>
public class WidenGapDialog : ComponentDialog
{
    public WidenGapDialog() : base(nameof(WidenGapDialog))
    {
        AddDialog(new TextPrompt("ObjectivesPrompt"));
        AddDialog(new TextPrompt("ProgressPrompt"));
        AddDialog(new WaterfallDialog("WidenGapWaterfall", new WaterfallStep[]
        {
            ObjectivesStepAsync,
            ProgressStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "WidenGapWaterfall";
    }

    private async Task<DialogTurnResult> ObjectivesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.PromptAsync("ObjectivesPrompt", new PromptOptions { Prompt = MessageFactory.Text("What are the key objectives for your business over the next 12 to 18 months?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> ProgressStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.TempObjectives = (string)stepContext.Result;
        return await stepContext.PromptAsync("ProgressPrompt", new PromptOptions { Prompt = MessageFactory.Text("Where are you right now against those objectives?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        // Progress is stored in ProspectProfile later
        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
