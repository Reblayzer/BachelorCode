using System;

namespace Application.Exceptions
{
  public sealed class ProviderNotRegisteredException : Exception
  {
    public ProviderNotRegisteredException(string message) : base(message) { }
  }
}
