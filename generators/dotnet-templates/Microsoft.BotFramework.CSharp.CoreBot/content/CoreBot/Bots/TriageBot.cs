using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreBot.Bots;

/// <summary>
/// This bot orchestrates the triage conversation by extending <see cref="DialogAndWelcomeBot{T}"/> with <see cref="TriageRootDialog"/>.
/// It leverages the welcome functionality and dialog management from the CoreBot template.
/// </summary>
public class TriageBot : DialogAndWelcomeBot<TriageRootDialog>
{
    private readonly IConfiguration _configuration;

    public TriageBot(ConversationState conversationState, UserState userState, TriageRootDialog dialog, ILogger<DialogBot<TriageRootDialog>> logger, IConfiguration configuration)
        : base(conversationState, userState, dialog, logger)
    {
        _configuration = configuration;
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded, 
        ITurnContext<IConversationUpdateActivity> turnContext, 
        CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                // Construct welcome message from config
                var companyName = _configuration["CompanyName"] ?? "our company";
                var welcomeMessageTemplate = _configuration["WelcomeMessage"] ?? "Hi! I'm TriageBot. Let's have a quick chat. Is now a good time?";
                var welcomeMessage = welcomeMessageTemplate.Replace("{CompanyName}", companyName);

                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
        }
    }
}
