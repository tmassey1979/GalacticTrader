namespace GalacticTrader.API.Contracts;

public sealed record RegisterPlayerApiRequest(
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
    string? Website = null);
