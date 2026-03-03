namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Defines authorization policies and roles for game features.
/// </summary>
public static class AuthorizationPolicies
{
    // Role definitions
    public const string AdminRole = "admin";
    public const string MapAdminRole = "map_admin";
    public const string ModeratorRole = "moderator";
    public const string PlayerRole = "player";
    public const string BotRole = "bot";

    // Policy names
    public const string PlayerPolicy = "PlayerPolicy";
    public const string ModeratorPolicy = "ModeratorPolicy";
    public const string AdminPolicy = "AdminPolicy";

    /// <summary>
    /// All available game roles.
    /// </summary>
    public static readonly string[] AllRoles = { AdminRole, MapAdminRole, ModeratorRole, PlayerRole, BotRole };

    /// <summary>
    /// Roles allowed to manage game.
    /// </summary>
    public static readonly string[] ManagementRoles = { AdminRole, MapAdminRole, ModeratorRole };
}
