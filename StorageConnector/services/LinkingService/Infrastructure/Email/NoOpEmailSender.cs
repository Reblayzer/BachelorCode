namespace LinkingService.Infrastructure.Email;

public sealed class NoOpEmailSender : IEmailSender
{
  public Task SendAsync(string to, string subject, string htmlBody) => Task.CompletedTask;
}
