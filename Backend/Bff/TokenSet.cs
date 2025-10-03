namespace Backend.Bff;

public sealed record TokenSet 
{ 
    public required string AccessToken { get; init; } 
    public string? RefreshToken { get; init; } 
    public DateTimeOffset ExpiresAtUtc { get; init; } 
}