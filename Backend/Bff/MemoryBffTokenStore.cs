using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Backend.Bff;

public sealed class MemoryBffTokenStore : IBffTokenStore
{
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(30);

    public MemoryBffTokenStore(IDistributedCache cache) => _cache = cache;

    public async Task SaveAsync(string sid, TokenSet tokens)
    {
        var json = JsonSerializer.Serialize(tokens);
        await _cache.SetStringAsync(Key(sid), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl
        });
    }

    public async Task<TokenSet?> GetAsync(string sid)
    {
        var json = await _cache.GetStringAsync(Key(sid));
        return json is null ? null : JsonSerializer.Deserialize<TokenSet>(json);
    }

    public Task DeleteAsync(string sid) => _cache.RemoveAsync(Key(sid));

    private static string Key(string sid) => $"bff:tokens:{sid}";
}