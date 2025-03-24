using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Two: Why Me, Why Now" from the triage call.
/// </summary>
public class WhyMeWhyNowDialog : ComponentDialog
{
    public WhyMeWhyNowDialog() : base(nameof(WhyMeWhyNowDialog))
    {
        AddDialog(new TextPrompt("ChallengesPrompt"));
        AddDialog(new WaterfallDialog("WhyMeWhyNowWaterfall", new WaterfallStep[]
        {
            ConfidentialityStepAsync,
            ChallengesStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "WhyMeWhyNowWaterfall";
    }

    private async Task<DialogTurnResult> ConfidentialityStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        await stepContext.Context.SendActivityAsync(MessageFactory.Text("First of all, I want you to know that everything you share with me is completely confidential. The more open you are, the easier it will be for us to find a solution. Does that make sense?"), cancellationToken);
        return await stepContext.NextAsync(cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> ChallengesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.PromptAsync("ChallengesPrompt", new PromptOptions { Prompt = MessageFactory.Text("Great... So what’s going on in your life and your business that made you want to schedule this call? Why now?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.TempKeyChallenges = (string)stepContext.Result;
        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
