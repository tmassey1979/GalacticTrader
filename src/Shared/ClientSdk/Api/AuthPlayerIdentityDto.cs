namespace GalacticTrader.Desktop.Api;

public sealed class AuthPlayerIdentityDto
{
    public Guid PlayerId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
