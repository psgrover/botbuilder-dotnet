using System.Collections.Generic;
using System.Threading.Tasks;
using CoreBot.Bots;
using CoreBot.Dialogs;
using CoreBot.Models;
using CoreBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CoreBot.Tests
{
    public class TriageRootDialogTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<OpenAIService> _openAIServiceMock;
        private readonly Mock<CluRecognizerService> _cluServiceMock;
        private readonly Mock<CrmService> _crmServiceMock;
        private readonly Mock<ISchedulingService> _schedulingServiceMock;
        private readonly Mock<EmailService> _emailServiceMock;

        private string _companyName = "CareTechPros";
        private string _expectedWelcomeMessage = string.Empty;

        public TriageRootDialogTests()
        {
            _expectedWelcomeMessage = $"Hi! I'm TriageBot from {_companyName}. Let's have a quick 15-minute chat to see if we can help you. Is now a good time?";
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["CompanyName"]).Returns(_companyName);
            _configurationMock.Setup(c => c["WelcomeMessage"]).Returns(_expectedWelcomeMessage);

            _openAIServiceMock = new Mock<OpenAIService>();
            _cluServiceMock = new Mock<CluRecognizerService>();
            _crmServiceMock = new Mock<CrmService>("mock-connection-string");
            _schedulingServiceMock = new Mock<ISchedulingService>();
            _emailServiceMock = new Mock<EmailService>();
        }

        [Fact]
        public async Task ShouldSendWelcomeMessageOnStart()
        {
            // Arrange
            var userState = new UserState(new MemoryStorage());
            var conversationState = new ConversationState(new MemoryStorage());
            var dialog = new TriageRootDialog(
                userState,
                _crmServiceMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _openAIServiceMock.Object,
                _cluServiceMock.Object,
                _schedulingServiceMock.Object
            );
            var testAdapter = new TestAdapter();
            var testClient = new DialogTestClient(testAdapter, dialog);

            // Act
            var reply = await testClient.SendActivityAsync<IActivity>("Hi") as Activity;

            // Assert
            Assert.Equal(_expectedWelcomeMessage, reply.Text);
        }

        [Fact]
        public async Task ShouldQualifyProspectAndOfferMeeting()
        {
            // Arrange
            var userState = new UserState(new MemoryStorage());
            var conversationState = new ConversationState(new MemoryStorage());
            var dialog = new TriageRootDialog(
                userState,
                _crmServiceMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _openAIServiceMock.Object,
                _cluServiceMock.Object,
                _schedulingServiceMock.Object
            );
            var testAdapter = new TestAdapter();
            var testClient = new DialogTestClient(testAdapter, dialog);

            // _cluServiceMock.Setup(c => c.RecognizeAsync("Yes"))
            //     .ReturnsAsync(new CluResult { TopIntent = "Confirm", Entities = new Dictionary<string, string>() });
            // _cluServiceMock.Setup(c => c.RecognizeAsync(_companyName))
            //     .ReturnsAsync(new CluResult { TopIntent = "HiringNeeds", Entities = new Dictionary<string, string> { { "HiringType", "contractors" } } });
            // _cluServiceMock.Setup(c => c.RecognizeAsync("contractors"))
            //     .ReturnsAsync(new CluResult { TopIntent = "HiringNeeds", Entities = new Dictionary<string, string> { { "HiringType", "contractors" } } });
            //_openAIServiceMock.Setup(o => o.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            //    .ReturnsAsync("Looks like you're a good fit! Ready to schedule?");
            _schedulingServiceMock.Setup(s => s.ScheduleMeetingAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync("https://calendly.com/test");

            // Act
            var replies = new List<string>();
            replies.Add((await testClient.SendActivityAsync<IActivity>("Hi") as Activity).Text); // Start
            replies.Add((await testClient.SendActivityAsync<IActivity>("Yes") as Activity).Text); // IntroDialog confirmation
            replies.Add((await testClient.SendActivityAsync<IActivity>(_companyName) as Activity).Text); // BusinessInfoDialog: Company
            replies.Add((await testClient.SendActivityAsync<IActivity>("50") as Activity).Text); // TeamSize
            replies.Add((await testClient.SendActivityAsync<IActivity>("contractors") as Activity).Text); // HiringType
            replies.Add((await testClient.SendActivityAsync<IActivity>("Yes") as Activity).Text); // BookingConfirmation

            // Assert
            Assert.Contains(_expectedWelcomeMessage, replies);
            Assert.Contains("Looks like you're a good fit! Ready to schedule?", replies);
            Assert.Contains("Please book a follow-up meeting here: https://calendly.com/test", replies);
            Assert.Contains("Awesome! We'll see you at the follow-up meeting.", replies);

            var userStateAccessor = userState.CreateProperty<ProspectProfile>("ProspectProfile");
            var turnContext = new TurnContext(testAdapter, new Activity());
            var profile = await userStateAccessor.GetAsync(turnContext, () => new ProspectProfile());
            Assert.True(profile.IsQualified);
            Assert.Equal(_companyName, profile.Company);
        }
    }
}