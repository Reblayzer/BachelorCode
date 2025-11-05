namespace Application.Interfaces
{
  public interface IConfirmationLinkGenerator
  {
    // Build an absolute URL for email confirmation. Keep the contract free of ASP.NET Core types
    string GenerateEmailConfirmationLink(string userId, string token, string scheme, string host);
    string GeneratePasswordResetLink(string email, string token, string scheme, string host);
  }
}
