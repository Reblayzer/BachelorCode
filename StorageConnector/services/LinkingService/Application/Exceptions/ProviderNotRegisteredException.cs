namespace LinkingService.Application.Exceptions
{
  public sealed class ProviderNotRegisteredException : Exception
  {
    public ProviderNotRegisteredException(string message) : base(message) { }
  }
}
