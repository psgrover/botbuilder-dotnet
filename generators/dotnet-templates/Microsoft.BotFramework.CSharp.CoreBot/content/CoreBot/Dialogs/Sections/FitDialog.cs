using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Nine: Fit; good news; schedule another call" from the triage call.
/// </summary>
public class FitDialog : ComponentDialog
{
    public FitDialog() : base(nameof(FitDialog))
    {
        AddDialog(new ConfirmPrompt("FitPrompt"));
        AddDialog(new WaterfallDialog("FitWaterfall", new WaterfallStep[]
        {
            FitStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "FitWaterfall";
    }

    private async Task<DialogTurnResult> FitStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        await stepContext.Context.SendActivityAsync(MessageFactory.Text(
            "So the good news is, I’m 100% certain we can help with everything you’ve said. The best thing now is to schedule another call to talk about how. Does that sound good?"
            ), cancellationToken);
        return await stepContext.PromptAsync("FitPrompt", new PromptOptions { Prompt = MessageFactory.Text("Are you ready to move forward with scheduling that call?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.IsQualified = (bool)stepContext.Result;
        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
