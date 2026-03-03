namespace GalacticTrader.Services.Auth;

public sealed record PlayerIdentity(Guid PlayerId, string Username, string Email, DateTimeOffset RegisteredAtUtc);
