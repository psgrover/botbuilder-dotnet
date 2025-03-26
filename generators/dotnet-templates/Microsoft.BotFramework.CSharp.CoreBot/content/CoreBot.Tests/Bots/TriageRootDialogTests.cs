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

        public TriageRootDialogTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["WelcomeMessage"]).Returns("Hi! I'm TriageBot from CareTechPros. Let's chat!");
            _configurationMock.Setup(c => c["CompanyName"]).Returns("CareTechPros");

            _openAIServiceMock = new Mock<OpenAIService>();
            _cluServiceMock = new Mock<CluRecognizerService>();
            _crmServiceMock = new Mock<CrmService>("mock-connection-string");
            _schedulingServiceMock = new Mock<ISchedulingService>();
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
                _configurationMock.Object,
                _openAIServiceMock.Object,
                _cluServiceMock.Object,
                _schedulingServiceMock.Object
            );
            var testAdapter = new TestAdapter();
            var testClient = new DialogTestClient(testAdapter, dialog);

            // Act
            var reply = await testClient.SendActivityAsync<string>("Hi");

            // Assert
            Assert.Equal("Hi! I'm TriageBot from CareTechPros. Let's chat!", reply.Text);
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
                _configurationMock.Object,
                _openAIServiceMock.Object,
                _cluServiceMock.Object,
                _schedulingServiceMock.Object
            );
            var testAdapter = new TestAdapter();
            var testClient = new DialogTestClient(testAdapter, dialog);

            _cluServiceMock.Setup(c => c.RecognizeAsync("Yes"))
                .ReturnsAsync(new CluResult { TopIntent = "Confirm", Entities = new Dictionary<string, string>() });
            _cluServiceMock.Setup(c => c.RecognizeAsync("AcmeCorp"))
                .ReturnsAsync(new CluResult { TopIntent = "HiringNeeds", Entities = new Dictionary<string, string> { { "HiringType", "contractors" } } });
            _cluServiceMock.Setup(c => c.RecognizeAsync("contractors"))
                .ReturnsAsync(new CluResult { TopIntent = "HiringNeeds", Entities = new Dictionary<string, string> { { "HiringType", "contractors" } } });
            _openAIServiceMock.Setup(o => o.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Looks like you’re a good fit! Ready to schedule?");
            _schedulingServiceMock.Setup(s => s.ScheduleMeetingAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync("https://calendly.com/test");

            // Act
            var replies = new List<string>();
            replies.Add((await testClient.SendActivityAsync<string>("Hi")).Text); // Start
            replies.Add((await testClient.SendActivityAsync<string>("Yes")).Text); // IntroDialog confirmation
            replies.Add((await testClient.SendActivityAsync<string>("AcmeCorp")).Text); // BusinessInfoDialog: Company
            replies.Add((await testClient.SendActivityAsync<string>("50")).Text); // TeamSize
            replies.Add((await testClient.SendActivityAsync<string>("contractors")).Text); // HiringType
            replies.Add((await testClient.SendActivityAsync<string>("Yes")).Text); // BookingConfirmation

            // Assert
            Assert.Contains("Hi! I'm TriageBot from CareTechPros. Let's chat!", replies);
            Assert.Contains("Looks like you’re a good fit! Ready to schedule?", replies);
            Assert.Contains("Please book a follow-up meeting here: https://calendly.com/test", replies);
            Assert.Contains("Awesome! We’ll see you at the follow-up meeting.", replies);

            var userStateAccessor = userState.CreateProperty<ProspectProfile>("ProspectProfile");
            var profile = await userStateAccessor.GetAsync(testClient.TurnContext, () => new ProspectProfile());
            Assert.True(profile.IsQualified);
            Assert.Equal("AcmeCorp", profile.Company);
        }
    }
}
