namespace CoreBot.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    public EmailService(IConfiguration configuration) => _configuration = configuration;

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
            Subject = $"{_configuration[SmtpSubject]}: {profile.Name}",
            Body = $"Prospect: {profile.Name}\nPhone: {profile.PhoneNumber}\nCall Time: {profile.PreferredCallTime}\nDetails: {profile.ConversationSummary}"
        });
    }
}
