using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CoreBot.Models;
using CoreBot.Services;

namespace CoreBot.Dialogs;

/// <summary>
/// The root dialog coordinates the triage sections and manages state.
/// Integrates CLU/GPT-4 for natural language understanding and generation.
/// Validates phone numbers and sends email notifications.
/// Adds a summary to the CRM/Database and notifies the team for follow-up.
/// </summary>
public class TriageRootDialog : ComponentDialog
{
    private readonly UserState _userState;
    private readonly CrmService _crmService;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly OpenAIService _openAIService;
    private readonly CluRecognizerService _cluService;
    private readonly ISchedulingService _schedulingService;
    
    private readonly string _companyName;

    public TriageRootDialog(UserState userState, CrmService crmService, EmailService emailService, 
    IConfiguration configuration, OpenAIService openAIService, CluRecognizerService cluService, 
    ISchedulingService schedulingService) 
    : base(nameof(TriageRootDialog))
    {
        _userState = userState;
        _crmService = crmService;
        _emailService = emailService;
        _configuration = configuration;
        _openAIService = openAIService;
        _cluService = cluService;
        _schedulingService = schedulingService;

        _companyName = _configuration["CompanyName"] ?? "our company";

        AddDialog(new IntroDialog());
        AddDialog(new WhyMeWhyNowDialog());
        AddDialog(new BusinessInfoDialog());
        AddDialog(new WidenGapDialog());
        AddDialog(new WhatsMissingDialog());
        AddDialog(new WhatDoYouNeedDialog());
        AddDialog(new TimingDialog());
        AddDialog(new ProblemCheckInDialog());
        AddDialog(new FitDialog());
        AddDialog(new ConfirmPrompt("BookingConfirmationPrompt"));
        AddDialog(new ConfirmPrompt("PhoneCallPrompt"));
        AddDialog(new TextPrompt("PhoneNumberPrompt", PhoneNumberValidatorAsync));
        AddDialog(new TextPrompt("CallTimePrompt"));

        AddDialog(new WaterfallDialog("TriageWaterfall", new WaterfallStep[]
        {
            IntroStepAsync,
            WhyMeWhyNowStepAsync,
            BusinessInfoStepAsync,
            WidenGapStepAsync,
            WhatsMissingStepAsync,
            WhatDoYouNeedStepAsync,
            TimingStepAsync,
            ProblemCheckInStepAsync,
            FitStepAsync,
            FinalizeStepAsync,
            PhoneCallStepAsync,
            PhoneNumberStepAsync,
            CallTimeStepAsync,
            NotifyTeamStepAsync
        }));

        InitialDialogId = "TriageWaterfall";
    }

    private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = await InitializeTriageSessionAsync(stepContext);
        triageSession.CurrentSection = "Intro";
        return await stepContext.BeginDialogAsync(nameof(IntroDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> WhyMeWhyNowStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "WhyMeWhyNow";
        return await stepContext.BeginDialogAsync(nameof(WhyMeWhyNowDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> BusinessInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result ?? (triageSession) stepContext.Options;
        triageSession.CurrentSection = "BusinessInfo";
        return await stepContext.BeginDialogAsync(nameof(BusinessInfoDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> WidenGapStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "WidenGap";
        return await stepContext.BeginDialogAsync(nameof(WidenGapDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> WhatsMissingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "WhatsMissing";
        return await stepContext.BeginDialogAsync(nameof(WhatsMissingDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> WhatDoYouNeedStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "WhatDoYouNeed";
        return await stepContext.BeginDialogAsync(nameof(WhatDoYouNeedDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> TimingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "Timing";
        return await stepContext.BeginDialogAsync(nameof(TimingDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> ProblemCheckInStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "ProblemCheckIn";
        return await stepContext.BeginDialogAsync(nameof(ProblemCheckInDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> FitStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        triageSession.CurrentSection = "Fit";
        return await stepContext.BeginDialogAsync(nameof(FitDialog), triageSession, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var triageSession = (TriageSession)stepContext.Result;
        var profileStateAccessors = _userState.CreateProperty<ProspectProfile>("ProspectProfile");
        var prospectProfile = await profileStateAccessors.GetAsync(stepContext.Context, () => new ProspectProfile(), cancellationToken);

        // Map TriageSession data to ProspectProfile
        prospectProfile.Name = triageSession.ProspectName;
        prospectProfile.Company = triageSession.TempCompany;
        prospectProfile.TeamSize = triageSession.TempTeamSize;
        prospectProfile.KeyChallenges = triageSession.TempKeyChallenges != null ? new List<string> { triageSession.TempKeyChallenges } : new List<string>();
        prospectProfile.Objectives = triageSession.TempObjectives != null ? new List<string> { triageSession.TempObjectives } : new List<string>();
        prospectProfile.DesiredOutcome = triageSession.TempDesiredOutcome;
        prospectProfile.Timeline = triageSession.TempTimeline;
        prospectProfile.DecisionMakers = triageSession.TempDecisionMakers;
        prospectProfile.IsQualified = triageSession.IsQualified;
        prospectProfile.ConversationSummary = $"Triage completed on {DateTime.UtcNow:yyyy-MM-dd}. Qualified: {triageSession.IsQualified}. Hiring needs: {triageSession.TempHiringType}.";

        if (triageSession.IsQualified)
        {
            var summary = $"Here’s what we discussed: You’re with {prospectProfile.Company}, needing {triageSession.TempHiringType} for {prospectProfile.DesiredOutcome}, targeting {prospectProfile.Timeline}. Let’s schedule a follow-up call with all decision-makers.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(summary), cancellationToken);
            
            // Use static meeting link from configuration
            //var meetingLink = _configuration["FollowUpMeetingLink"] ?? "https://calendly.com/caretechpros/gameplan";

            //Use dynamic meeting link from scheduling service
            var meetingLink = await _schedulingService.ScheduleMeetingAsync(prospectProfile.Name, prospectProfile.DecisionMakers);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Please book a follow up meeting here: {meetingLink}"), cancellationToken);

            await stepContext.PromptAsync("BookingConfirmationPrompt", 
            new PromptOptions
            { 
                Prompt = MessageFactory.Text("Have you booked your follow-up meeting slot? (Yes/No)"),
                RetryPrompt = MessageFactory.Text("Please let me know if you’ve booked the slot by answering 'Yes' or 'No'.") 
            }, cancellationToken);
        }
        else
        {
            var context = $"Prospect {prospectProfile.Name} from {prospectProfile.Company} didn’t qualify.";
            var gptResponse = await _openAIService.GenerateResponseAsync("Politely disqualify and suggest alternatives.", context);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(gptResponse), cancellationToken);
        }

        //Save to CRM
        await _crmService.SaveProspectProfileAsync(prospectProfile);

        await profileStateAccessors.SetAsync(stepContext.Context, prospectProfile, cancellationToken);
        await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);

        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
    }

    private async Task<DialogTurnResult> PhoneCallStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var booked = (bool)stepContext.Result;
        var profileStateAccessors = _userState.CreateProperty<ProspectProfile>("ProspectProfile");
        var prospectProfile = await profileStateAccessors.GetAsync(stepContext.Context, () => new ProspectProfile(), cancellationToken);

        if (booked)
        {
            prospectProfile.IsFollowUpBooked = true;
            prospectProfile.ConversationSummary += " Follow-up booked via link.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Awesome! We’ll see you at the follow-up meeting."), cancellationToken);
            await _crmService.SaveProspectProfileAsync(prospectProfile);
            await profileStateAccessors.SetAsync(stepContext.Context, prospectProfile, cancellationToken);
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        else
        {
            var context = $"Prospect {prospectProfile.Name} declined booking.";
            var gptResponse = await _openAIService.GenerateResponseAsync($"Ask if they’d prefer a phone call from {_companyName}.", context);

            return await stepContext.PromptAsync("PhoneCallPrompt", new PromptOptions
            {
                Prompt = MessageFactory.Text(gptResponse)
                RetryPrompt = MessageFactory.Text("Please say 'Yes' or 'No' to let me know if you’d like a call.")
            }, cancellationToken);
        }
    }

    private async Task<DialogTurnResult> PhoneNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var wantsCall = (bool)stepContext.Result;
        var profileStateAccessors = _userState.CreateProperty<ProspectProfile>("ProspectProfile");
        var prospectProfile = await profileStateAccessors.GetAsync(stepContext.Context, () => new ProspectProfile(), cancellationToken);

        if (wantsCall)
        {
            return await stepContext.PromptAsync("PhoneNumberPrompt", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide a valid US phone number (e.g., 123-456-7890) where we can reach you."),
                RetryPrompt = MessageFactory.Text("That doesn’t look like a valid US phone number. Please enter it in the format 123-456-7890.")
            }, cancellationToken);
        }
        else
        {
            prospectProfile.IsFollowUpBooked = false;
            prospectProfile.ConversationSummary += " Declined follow-up booking and phone call.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("No problem! Feel free to book the meeting later or reach out if you need assistance."), cancellationToken);
            await _crmService.SaveProspectProfileAsync(prospectProfile);
            await profileStateAccessors.SetAsync(stepContext.Context, prospectProfile, cancellationToken);
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task<DialogTurnResult> CallTimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var phoneNumber = (string)stepContext.Result;
        var profileStateAccessors = _userState.CreateProperty<ProspectProfile>("ProspectProfile");
        var prospectProfile = await profileStateAccessors.GetAsync(stepContext.Context, () => new ProspectProfile(), cancellationToken);

        prospectProfile.PhoneNumber = phoneNumber;

        return await stepContext.PromptAsync("CallTimePrompt", new PromptOptions
        {
            Prompt = MessageFactory.Text("When’s the best day and time for us to call you? (e.g., 'Wednesday at 2 PM PST')"),
            RetryPrompt = MessageFactory.Text("Please provide a specific day and time, like 'Wednesday at 2 PM PST'.")
        }, cancellationToken);
    }

    private async Task<DialogTurnResult> NotifyTeamStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var callTime = (string)stepContext.Result;
        var profileStateAccessors = _userState.CreateProperty<ProspectProfile>("ProspectProfile");
        var prospectProfile = await profileStateAccessors.GetAsync(stepContext.Context, () => new ProspectProfile(), cancellationToken);

        prospectProfile.PreferredCallTime = callTime;
        prospectProfile.IsFollowUpBooked = false; // Not booked via link, but team will follow up
        prospectProfile.ConversationSummary += $" Requested a call at {callTime} to {prospectProfile.PhoneNumber}.";

        var summary = $"Here’s what we discussed: You’re with {prospectProfile.Company}, needing {prospectProfile.DesiredOutcome}, and we’ll call you at {prospectProfile.PhoneNumber} on {callTime}.";
        await stepContext.Context.SendActivityAsync(MessageFactory.Text(summary), cancellationToken);

        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I’ve informed the team to reach out to you at {prospectProfile.PhoneNumber} on {callTime}."), cancellationToken);

        // Team notification via email and CRM
        await _emailService.SendNotificationAsync(prospectProfile);
        await _crmService.SaveProspectProfileAsync(prospectProfile);
        await profileStateAccessors.SetAsync(stepContext.Context, prospectProfile, cancellationToken);
        await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);

        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
    }

    private async Task<bool> PhoneNumberValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
    {
        var phoneNumber = promptContext.Recognized.Value?.Trim();
        if (string.IsNullOrEmpty(phoneNumber)) return false;
 
        // Validate US phone number (e.g., 123-456-7890, (123) 456-7890, or 1234567890)
        var regex = new Regex(@"^(\d{10}|\d{3}-\d{3}-\d{4}|\(\d{3}\)\s*\d{3}-\d{4})$");
        return await Task.FromResult(regex.IsMatch(phoneNumber));
    }

    private async Task<TriageSession> InitializeTriageSessionAsync(WaterfallStepContext stepContext)
    {
        var userStateAccessors = _userState.CreateProperty<TriageSession>("TriageSession");
        return await userStateAccessors.GetAsync(stepContext.Context, () => new TriageSession(), cancellationToken);
    }
}
