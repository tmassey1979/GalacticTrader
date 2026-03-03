namespace GalacticTrader.API.Contracts;

public sealed record RegisterPlayerApiRequest(string Username, string Email, string Password);
