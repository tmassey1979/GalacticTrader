namespace GalacticTrader.Desktop.Api;

public sealed class AuthLoginResultDto
{
    public AuthPlayerIdentityDto Player { get; init; } = new();
    public string AccessToken { get; init; } = string.Empty;
}
