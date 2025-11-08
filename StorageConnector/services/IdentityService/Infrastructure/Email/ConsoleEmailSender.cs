namespace IdentityService.Infrastructure.Email;

public sealed class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        Console.WriteLine($"[EMAIL to {to}] {subject}\n{htmlBody}");
        return Task.CompletedTask;
    }
}
