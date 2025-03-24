using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Seven: How soon do you need this?" from the triage call.
/// </summary>
public class TimingDialog : ComponentDialog
{
    public TimingDialog() : base(nameof(TimingDialog))
    {
        AddDialog(new TextPrompt("TimelinePrompt"));
        AddDialog(new WaterfallDialog("TimingWaterfall", new WaterfallStep[]
        {
            TimelineStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "TimingWaterfall";
    }

    private async Task<DialogTurnResult> TimelineStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.PromptAsync("TimelinePrompt", 
        new PromptOptions 
        { 
            Prompt = MessageFactory.Text("Just to get an idea of timescales, how soon would you like to get this addressed?") 
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.TempTimeline = (string)stepContext.Result;
        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
