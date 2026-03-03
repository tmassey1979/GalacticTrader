namespace GalacticTrader.Services.Auth;

public sealed record RegisterPlayerRequest(
    string Username,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null,
    string? MiddleName = null,
    string? Nickname = null,
    DateOnly? Birthdate = null,
    string? Gender = null,
    string? Pronouns = null,
    string? PhoneNumber = null,
    string? Locale = null,
    string? TimeZone = null,
    string? Website = null,
    Guid? PlayerId = null);
