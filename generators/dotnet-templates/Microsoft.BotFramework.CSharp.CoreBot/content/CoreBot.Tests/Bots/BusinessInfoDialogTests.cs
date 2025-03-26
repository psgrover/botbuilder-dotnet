using System.Collections.Generic;
using System.Threading.Tasks;
using CoreBot.Dialogs.Sections;
using CoreBot.Models;
using CoreBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Moq;
using Xunit;

namespace CoreBot.Tests
{
    public class BusinessInfoDialogTests
    {
        private readonly Mock<OpenAIService> _openAIServiceMock;
        private readonly Mock<CluRecognizerService> _cluServiceMock;

        public BusinessInfoDialogTests()
        {
            _openAIServiceMock = new Mock<OpenAIService>();
            _cluServiceMock = new Mock<CluRecognizerService>();
        }

        [Fact]
        public async Task ShouldCollectBusinessInfo()
        {
            // Arrange
            var dialog = new BusinessInfoDialog(_openAIServiceMock.Object, _cluServiceMock.Object);
            var testAdapter = new TestAdapter();
            var triageSession = new TriageSession();
            var testClient = new DialogTestClient(testAdapter, dialog, triageSession);

            _cluServiceMock.Setup(c => c.RecognizeAsync("contractors"))
                .ReturnsAsync(new CluResult { TopIntent = "HiringNeeds", Entities = new Dictionary<string, string> { { "HiringType", "contractors" } } });
            _openAIServiceMock.Setup(o => o.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Got it, you need contractors at AcmeCorp.");

            // Act
            var replies = new List<string>();
            replies.Add((await testClient.SendActivityAsync<string>("AcmeCorp")).Text); // Company
            replies.Add((await testClient.SendActivityAsync<string>("50")).Text); // TeamSize
            replies.Add((await testClient.SendActivityAsync<string>("contractors")).Text); // HiringType

            // Assert
            Assert.Contains("How many employees are currently at AcmeCorp?", replies);
            Assert.Contains("Are you looking to hire full-time employees, contractors, or both?", replies);
            Assert.Contains("Got it, you need contractors at AcmeCorp.", replies);

            Assert.Equal("AcmeCorp", triageSession.TempCompany);
            Assert.Equal(50, triageSession.TempTeamSize);
            Assert.Equal("contractors", triageSession.TempHiringType);
        }
    }
}
