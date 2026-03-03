namespace GalacticTrader.Services.Auth;

public sealed record PlayerSession(PlayerIdentity Player, string AccessToken, DateTimeOffset ExpiresAtUtc);
