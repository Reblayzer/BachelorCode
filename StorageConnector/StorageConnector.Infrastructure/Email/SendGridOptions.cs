namespace StorageConnector.Infrastructure.Email;

public class SendGridOptions
{
    public string ApiKey { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public string FromName  { get; set; } = "StorageConnector";
}