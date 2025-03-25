using System.Threading;
using System.Threading.Tasks;
using CoreBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// The introductory dialog asks for the prospect's name and confirms the time.
/// </summary>
public class IntroDialog : ComponentDialog
{
    public IntroDialog() 
    : base(nameof(IntroDialog))
    {
        AddDialog(new TextPrompt("NamePrompt"));
        AddDialog(new ConfirmPrompt("TimePrompt"));
        AddDialog(new WaterfallDialog("IntroWaterfall", new WaterfallStep[] { AskNameStepAsync, ConfirmTimeStepAsync, ExplainCallStepAsync }));

        InitialDialogId = "IntroWaterfall";
    }

    private async Task<DialogTurnResult> AskNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        return await stepContext.PromptAsync("NamePrompt", new PromptOptions { Prompt = MessageFactory.Text("Hi! I'm from CareTechPros. What's your name?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> ConfirmTimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.ProspectName = (string)stepContext.Result;
        return await stepContext.PromptAsync("TimePrompt", new PromptOptions { Prompt = MessageFactory.Text($"Hey {triageSession.ProspectName}, is now still a good time for a quick 15-minute chat?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> ExplainCallStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.IsTimeConfirmed = (bool)stepContext.Result;
        if (triageSession.IsTimeConfirmed)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Great, thanks for being on time! My job today is to ask a few questions to see if we can help you. If we can, we'll schedule another call. If not, I'll point you in the right direction. Sound good?"), cancellationToken);
            return await stepContext.EndDialogAsync(triageSession, cancellationToken);
        }

        return await stepContext.EndDialogAsync(null, cancellationToken);
    }
}
