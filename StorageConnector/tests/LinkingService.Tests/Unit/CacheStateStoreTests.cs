using LinkingService.Infrastructure.Stores;
using Microsoft.Extensions.Caching.Memory;
using LinkingService.Domain;

namespace LinkingService.Tests.Unit;

public sealed class CacheStateStoreTests
{
  private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

  [Fact]
  public async Task SaveAndTake_ReturnsStoredValue_And_RemovesIt()
  {
    var memory = new MemoryCache(new MemoryCacheOptions());
    var store = new CacheStateStore(memory);

    await store.SaveAsync("s-1", TestUserId, "verifier-1", ProviderType.Google, TimeSpan.FromMinutes(5));

    var taken = await store.TakeAsync("s-1");
    Assert.NotNull(taken);
    Assert.Equal(TestUserId, taken.Value.userId);
    Assert.Equal("verifier-1", taken.Value.codeVerifier);
    Assert.Equal(ProviderType.Google, taken.Value.provider);

    var second = await store.TakeAsync("s-1");
    Assert.Null(second);
  }
}
