using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CoreBot.Models;
using Microsoft.Extensions.Configuration;

namespace CoreBot.Services;

public class EmailService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public async Task SendNotificationAsync(ProspectProfile profile)
    {
        var smtpClient = new SmtpClient(_configuration["SmtpHost"])
        {
            Port = int.Parse(_configuration["SmtpPort"]),
            Credentials = new NetworkCredential(_configuration["SmtpUser"], _configuration["SmtpPassword"]),
            EnableSsl = _configuration["SmtpEnableSsl"] == "true"
        };
        await smtpClient.SendMailAsync(new MailMessage
        {
            From = new MailAddress(_configuration["SmtpFromName"] ?? "bot@caretechpros.com"),
            To = { "info@caretechpros.com" },
            Subject = $"{_configuration["SmtpSubject"]}: {profile.Name}",
            Body = $"Prospect: {profile.Name}\nPhone: {profile.PhoneNumber}\nCall Time: {profile.PreferredCallTime}\nDetails: {profile.ConversationSummary}"
        });
    }
}
