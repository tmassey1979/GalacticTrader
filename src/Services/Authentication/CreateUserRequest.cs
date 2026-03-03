namespace GalacticTrader.Services.Authentication;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to create a new user account.
/// </summary>
public class CreateUserRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
