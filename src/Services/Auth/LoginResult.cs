namespace GalacticTrader.Services.Auth;

public sealed record LoginResult(PlayerIdentity Player, string AccessToken, DateTimeOffset ExpiresAtUtc);
