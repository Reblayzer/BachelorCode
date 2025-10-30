using System;
using System.Threading.Tasks;
using Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using Domain;

namespace Tests.Stores;

public sealed class CacheStateStoreTests
{
  [Fact]
  public async Task SaveAndTake_ReturnsStoredValue_And_RemovesIt()
  {
    var memory = new MemoryCache(new MemoryCacheOptions());
    var store = new CacheStateStore(memory);

    await store.SaveAsync("s-1", "verifier-1", ProviderType.Google, TimeSpan.FromMinutes(5));

    var taken = await store.TakeAsync("s-1");
    Assert.NotNull(taken);
    Assert.Equal("verifier-1", taken?.codeVerifier);
    Assert.Equal(ProviderType.Google, taken?.provider);

    var second = await store.TakeAsync("s-1");
    Assert.Null(second);
  }
}
