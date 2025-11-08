using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LinkingService.Infrastructure.Email;

public sealed class SendGridEmailSender : IEmailSender
{
    private readonly SendGridClient _client;
    private readonly SendGridOptions _opt;

    public SendGridEmailSender(IOptions<SendGridOptions> opt)
    {
        _opt = opt.Value;
        _client = new SendGridClient(_opt.ApiKey);
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_opt.FromEmail, _opt.FromName),
            Subject = subject,
            HtmlContent = htmlBody
        };
        msg.AddTo(new EmailAddress(to));
        var resp = await _client.SendEmailAsync(msg);
        if ((int)resp.StatusCode >= 400)
            throw new InvalidOperationException($"SendGrid failed: {resp.StatusCode}");
    }
}
