namespace GalacticTrader.Services.Auth;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    private readonly ConcurrentDictionary<string, RegisteredAccount> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RegisteredAccount> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SessionRecord> _sessionsByToken = new(StringComparer.Ordinal);

    public Task<PlayerIdentity> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedUsername = Normalize(request.Username);
        var normalizedEmail = Normalize(request.Email);
        if (_usersByUsername.ContainsKey(normalizedUsername))
        {
            throw new InvalidOperationException("Username is already registered.");
        }

        if (_usersByEmail.ContainsKey(normalizedEmail))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var identity = new PlayerIdentity(
            PlayerId: Guid.NewGuid(),
            Username: normalizedUsername,
            Email: normalizedEmail,
            RegisteredAtUtc: DateTimeOffset.UtcNow);

        var account = new RegisteredAccount
        {
            Identity = identity,
            PasswordHash = HashPassword(request.Password)
        };

        if (!_usersByUsername.TryAdd(normalizedUsername, account))
        {
            throw new InvalidOperationException("Username is already registered.");
        }

        if (!_usersByEmail.TryAdd(normalizedEmail, account))
        {
            _usersByUsername.TryRemove(normalizedUsername, out _);
            throw new InvalidOperationException("Email is already registered.");
        }

        return Task.FromResult(identity);
    }

    public Task<LoginResult?> LoginAsync(LoginPlayerRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedUsername = Normalize(request.Username);
        if (!_usersByUsername.TryGetValue(normalizedUsername, out var account))
        {
            return Task.FromResult<LoginResult?>(null);
        }

        var suppliedHash = HashPassword(request.Password);
        if (!string.Equals(account.PasswordHash, suppliedHash, StringComparison.Ordinal))
        {
            return Task.FromResult<LoginResult?>(null);
        }

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("+", "-", StringComparison.Ordinal)
            .TrimEnd('=');

        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);
        var session = new SessionRecord
        {
            Player = account.Identity,
            AccessToken = token,
            ExpiresAtUtc = expiresAt
        };

        _sessionsByToken[token] = session;
        CleanupExpiredSessions();

        return Task.FromResult<LoginResult?>(new LoginResult(session.Player, token, expiresAt));
    }

    public Task<PlayerSession?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedToken = accessToken?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return Task.FromResult<PlayerSession?>(null);
        }

        if (!_sessionsByToken.TryGetValue(normalizedToken, out var session))
        {
            return Task.FromResult<PlayerSession?>(null);
        }

        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _sessionsByToken.TryRemove(normalizedToken, out _);
            return Task.FromResult<PlayerSession?>(null);
        }

        return Task.FromResult<PlayerSession?>(new PlayerSession(session.Player, normalizedToken, session.ExpiresAtUtc));
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }

    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private void CleanupExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredTokens = _sessionsByToken
            .Where(entry => entry.Value.ExpiresAtUtc <= now)
            .Select(entry => entry.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _sessionsByToken.TryRemove(token, out _);
        }
    }

    private sealed class RegisteredAccount
    {
        public required PlayerIdentity Identity { get; init; }

        public required string PasswordHash { get; init; }
    }

    private sealed class SessionRecord
    {
        public required PlayerIdentity Player { get; init; }

        public required string AccessToken { get; init; }

        public required DateTimeOffset ExpiresAtUtc { get; init; }
    }
}
