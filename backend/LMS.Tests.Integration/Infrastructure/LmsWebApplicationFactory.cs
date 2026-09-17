using LMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LMS.Tests.Integration.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that replaces Azure AD JWT authentication with the
/// <see cref="TestAuthHandler"/> and swaps the PostgreSQL database for an in-memory one.
///
/// Usage:
/// <code>
///   await using var factory = new LmsWebApplicationFactory();
///   var client = factory.CreateClient();
/// </code>
/// </summary>
public class LmsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide required environment variables so Program.cs does not throw.
        builder.UseSetting("DATABASE_URL", "Host=ignored;Database=ignored;Username=ignored;Password=ignored");

        builder.ConfigureServices(services =>
        {
            // ── Replace real DbContext with in-memory database ────────────────
            services.RemoveAll<DbContextOptions<LmsDbContext>>();
            services.RemoveAll<LmsDbContext>();

            var dbName = $"LmsTestDb_{Guid.NewGuid()}";
            services.AddDbContext<LmsDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            // ── Replace Azure AD JWT Bearer with the test handler ─────────────
            // Remove the default JWT Bearer registration added in Program.cs.
            services.RemoveAll<IAuthenticationSchemeProvider>();

            services.AddAuthentication(TestAuthScheme.Name)
                .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthScheme.Name, _ => { });

            // Re-add authorization policies that the controllers rely on.
            services.AddAuthorization(options =>
            {
                options.AddPolicy("HRAdminOrSuperAdmin", policy =>
                    policy.RequireRole(RoleNames.HRAdmin, RoleNames.SuperAdmin));
            });
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Provides a shared factory instance across all tests in a collection,
/// avoiding the overhead of spinning up a new host per test class.
/// </summary>
[CollectionDefinition(nameof(LmsIntegrationCollection))]
public class LmsIntegrationCollection : ICollectionFixture<LmsWebApplicationFactory> { }
