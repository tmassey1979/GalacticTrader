namespace GalacticTrader.Services.Authentication;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to update user profile.
/// </summary>
public class UpdateUserRequest
{
    [MinLength(3)]
    public string? FirstName { get; set; }

    [MinLength(3)]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}
