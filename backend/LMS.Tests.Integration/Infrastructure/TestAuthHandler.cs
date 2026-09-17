using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.Tests.Integration.Infrastructure;

/// <summary>
/// Test authentication scheme name used throughout integration tests.
/// </summary>
public static class TestAuthScheme
{
    public const string Name = "TestAuth";

    /// <summary>Header name callers set to drive the handler's behavior.</summary>
    public const string RoleHeader = "X-Test-Role";
    public const string FailHeader = "X-Test-Auth-Fail";
    public const string FailReasonHeader = "X-Test-Fail-Reason";

    public const string FailReasonExpired = "expired";
    public const string FailReasonInvalidSignature = "invalid_signature";
}

/// <summary>
/// Options bag for <see cref="TestAuthHandler"/>.
/// </summary>
public class TestAuthHandlerOptions : AuthenticationSchemeOptions { }

/// <summary>
/// Fake authentication handler that replaces Azure AD JWT validation in integration tests.
///
/// Callers control the outcome by setting HTTP request headers:
///   X-Test-Auth-Fail       — present → simulate an auth failure (401)
///   X-Test-Fail-Reason     — "expired" | "invalid_signature" (determines WWW-Authenticate detail)
///   X-Test-Role            — comma-separated role names for the synthetic principal (e.g. "HRAdmin")
///
/// When neither failure header is set the handler returns a valid authenticated principal
/// carrying whatever roles are listed in X-Test-Role (defaults to "Employee").
/// </summary>
public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<TestAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // --- Simulate authentication failure ---
        if (Request.Headers.ContainsKey(TestAuthScheme.FailHeader))
        {
            var reason = Request.Headers[TestAuthScheme.FailReasonHeader].FirstOrDefault()
                         ?? "unauthorized";

            var detail = reason switch
            {
                TestAuthScheme.FailReasonExpired => "IDX10223: Lifetime validation failed. The token is expired.",
                TestAuthScheme.FailReasonInvalidSignature => "IDX10511: Signature validation failed.",
                _ => "Token validation failed."
            };

            return Task.FromResult(AuthenticateResult.Fail(detail));
        }

        // --- Build a synthetic authenticated principal ---
        var roles = Request.Headers[TestAuthScheme.RoleHeader].FirstOrDefault()
                    ?? RoleNames.Employee;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-oid-001"),
            new("oid", "test-user-oid-001"),
            new("preferred_username", "testuser@lms-test.onmicrosoft.com"),
            new("name", "Test User"),
        };

        foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim("roles", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, TestAuthScheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthScheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Convenience constants matching the domain role names.
/// Keeps tests free of magic strings.
/// </summary>
internal static class RoleNames
{
    internal const string HRAdmin = "HRAdmin";
    internal const string Employee = "Employee";
    internal const string Manager = "Manager";
    internal const string SuperAdmin = "SuperAdmin";
    internal const string Director = "Director";
}
