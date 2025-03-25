using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.Sections;

/// <summary>
/// Handles "Section Five: What's Missing or broken?" from the triage call.
/// </summary>
public class WhatsMissingDialog : ComponentDialog
{
    public WhatsMissingDialog() : base(nameof(WhatsMissingDialog))
    {
        AddDialog(new TextPrompt("MissingPrompt"));
        AddDialog(new ConfirmPrompt("BudgetPrompt"));
        AddDialog(new TextPrompt("DecisionMakersPrompt"));
        AddDialog(new WaterfallDialog("WhatsMissingWaterfall", new WaterfallStep[]
        {
            MissingStepAsync,
            BudgetStepAsync,
            DecisionMakersStepAsync,
            FinalizeStepAsync
        }));

        InitialDialogId = "WhatsMissingWaterfall";
    }

    private async Task<DialogTurnResult> MissingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.PromptAsync("MissingPrompt", new PromptOptions { Prompt = MessageFactory.Text("What do you need to fix or see more of in your business to hit those objectives?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> BudgetStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // MissingSkillsOrResources stored in ProspectProfile later
        return await stepContext.PromptAsync("BudgetPrompt", new PromptOptions { Prompt = MessageFactory.Text("Do you currently have the budget available to invest in a solution?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> DecisionMakersStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;

        // Budget stored in ProspectProfile later
        return await stepContext.PromptAsync("DecisionMakersPrompt", new PromptOptions { Prompt = MessageFactory.Text("Other than you, who else would need to be involved in any decision to invest in a solution?") }, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Options;
        triageSession.TempDecisionMakers.Add((string)stepContext.Result);
        return await stepContext.EndDialogAsync(triageSession, cancellationToken);
    }
}
