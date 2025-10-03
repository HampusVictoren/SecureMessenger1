using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Backend.Bff;

public interface IBffTokenRefresher
{
    Task<(bool ok, TokenSet tokens)> EnsureValidTokensAsync(string sid, TokenSet tokens, CancellationToken ct = default);
}

public sealed class BffTokenRefresher : IBffTokenRefresher
{
    private static readonly TimeSpan Skew = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBffTokenStore _store;
    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptions;
    private readonly ILogger<BffTokenRefresher> _logger;

    public BffTokenRefresher(
        IHttpClientFactory httpClientFactory,
        IBffTokenStore store,
        IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
        ILogger<BffTokenRefresher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _store = store;
        _oidcOptions = oidcOptions;
        _logger = logger;
    }

    public async Task<(bool ok, TokenSet tokens)> EnsureValidTokensAsync(string sid, TokenSet tokens, CancellationToken ct = default)
    {
        // If still valid (with skew), keep as-is
        if (tokens.ExpiresAtUtc > DateTimeOffset.UtcNow.Add(Skew))
            return (true, tokens);

        if (string.IsNullOrEmpty(tokens.RefreshToken))
        {
            _logger.LogInformation("No refresh token available for sid {sid}.", sid);
            return (false, tokens);
        }

        var options = _oidcOptions.Get("bff-oidc");
        var authority = options.Authority?.TrimEnd('/');
        if (string.IsNullOrEmpty(authority))
        {
            _logger.LogError("OIDC authority is not configured.");
            return (false, tokens);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = tokens.RefreshToken!,
                ["client_id"] = options.ClientId!
                // No client_secret for public client; no scope required for refresh in OpenIddict.
            });

            using var resp = await client.PostAsync($"{authority}/connect/token", content, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Refresh token failed for sid {sid}. Status: {status}. Body: {body}", sid, resp.StatusCode, err);
                return (false, tokens);
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var accessToken = root.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
            var newRefresh = root.TryGetProperty("refresh_token", out var rtEl) ? rtEl.GetString() : tokens.RefreshToken;
            var expiresIn = root.TryGetProperty("expires_in", out var eiEl) && eiEl.TryGetInt32(out var seconds) ? seconds : 300;

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Refresh response missing access_token for sid {sid}.", sid);
                return (false, tokens);
            }

            var updated = new TokenSet
            {
                AccessToken = accessToken!,
                RefreshToken = newRefresh,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
            };

            await _store.SaveAsync(sid, updated);
            _logger.LogInformation("Successfully refreshed tokens for sid {sid}.", sid);

            return (true, updated);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tokens for sid {sid}.", sid);
            return (false, tokens);
        }
    }
}