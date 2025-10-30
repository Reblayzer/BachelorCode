using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LinkingService.Http;

public sealed class RetryHandler : DelegatingHandler
{
  private readonly ILogger<RetryHandler> _logger;
  private readonly int _maxAttempts;

  public RetryHandler(ILogger<RetryHandler> logger, int maxAttempts = 3)
  {
    _logger = logger;
    _maxAttempts = Math.Max(1, maxAttempts);
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    var attempt = 0;
    for (; ; )
    {
      attempt++;
      try
      {
        var resp = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if ((int)resp.StatusCode >= 500 && attempt < _maxAttempts)
        {
          _logger.LogWarning("Transient server error {StatusCode} on attempt {Attempt} - retrying", resp.StatusCode, attempt);
          await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
          continue;
        }

        return resp;
      }
      catch (HttpRequestException ex) when (attempt < _maxAttempts)
      {
        _logger.LogWarning(ex, "HttpRequestException on attempt {Attempt} - retrying", attempt);
        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
        continue;
      }
    }
  }
}
