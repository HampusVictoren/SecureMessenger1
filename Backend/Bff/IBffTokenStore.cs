namespace Backend.Bff;

public interface IBffTokenStore
{
    Task SaveAsync(string sid, TokenSet tokens);
    Task<TokenSet?> GetAsync(string sid);
    Task DeleteAsync(string sid);
}