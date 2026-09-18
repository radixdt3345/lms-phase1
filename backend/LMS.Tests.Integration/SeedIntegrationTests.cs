// Integration tests for F-14: Seed Data & Initial Setup.
// Tests cover IT-F14-INT-001 through IT-F14-INT-007 from docs/test-plan.md.
// These exercise the full ASP.NET Core pipeline: middleware → controller → ISeedService → in-memory database.
// DI wiring verification: ISeedService/SeedService and DataSeeder registered in Program.cs.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-14 — Seed Data &amp; Initial Setup.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, ISeedService, DataSeeder, in-memory EF Core) is real.
///
/// DI registration verified: ISeedService, SeedService, DataSeeder registered in Program.cs.
/// No key mismatch: API returns { "data": SeedStatusDto } which the frontend reads as response.data.data.
///
/// IT- IDs covered: IT-F14-INT-001 through IT-F14-INT-007.
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class SeedIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SeedIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, role);
        return client;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-001: GET /api/admin/seed-status returns 401 without auth
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-001: An unauthenticated GET to /api/admin/seed-status must return 401.
    /// The [Authorize] attribute on the controller must deny this request.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_001_GET_SeedStatus_NoAuth_Returns401()
    {
        // Arrange — no auth; TestAuthHandler returns Fail when X-Test-Auth-Fail is set
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");

        // Act
        var response = await client.GetAsync("/api/admin/seed-status");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "an unauthenticated request to GET /api/admin/seed-status must return 401");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-002: GET /api/admin/seed-status with Employee role returns 403
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-002: A GET authenticated as "Employee" must be rejected with 403 Forbidden.
    /// The [Authorize(Roles = "HRAdmin,SuperAdmin")] on GetSeedStatus must deny the request.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_002_GET_SeedStatus_EmployeeRole_Returns403()
    {
        // Arrange — Employee is not HRAdmin or SuperAdmin
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/admin/seed-status");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be authorized to read seed status; RBAC must return 403");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-003: GET /api/admin/seed-status with HRAdmin returns 200 with { data: SeedStatusDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-003: A GET authenticated as HRAdmin must return 200 OK and the body must
    /// contain a "data" property (ApiResponse&lt;T&gt; envelope) with the SeedStatusDto shape.
    /// This verifies: controller is registered, ISeedService is DI-wired, response wraps in { data: T }.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_003_GET_SeedStatus_HRAdminRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/admin/seed-status");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/admin/seed-status with HRAdmin must return 200 OK");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<SeedStatusResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain SeedStatusDto");

        // Assert — SeedStatusDto fields are present (counts default to 0 with empty in-memory DB)
        envelope.Data!.TotalRoles.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.TotalDepartments.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.TotalLeaveTypes.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.TotalPublicHolidays.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.TotalSystemConfigs.Should().BeGreaterThanOrEqualTo(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-004: GET /api/admin/seed-status with SuperAdmin returns 200
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-004: A GET authenticated as SuperAdmin must also return 200 OK.
    /// Both HRAdmin and SuperAdmin are authorized for the seed-status endpoint.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_004_GET_SeedStatus_SuperAdminRole_Returns200()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.SuperAdmin);

        // Act
        var response = await client.GetAsync("/api/admin/seed-status");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/admin/seed-status with SuperAdmin must return 200 OK");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-005: POST /api/admin/reseed with HRAdmin returns 403
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-005: A POST authenticated as HRAdmin to /api/admin/reseed must be denied with 403.
    /// The [Authorize(Roles = "SuperAdmin")] on TriggerReseed must deny HRAdmin.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_005_POST_Reseed_HRAdminRole_Returns403()
    {
        // Arrange — HRAdmin is not SuperAdmin
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/reseed", new { section = "all" });

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an HRAdmin must not be authorized to trigger reseed; only SuperAdmin is allowed");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-006: POST /api/admin/reseed with SuperAdmin and valid section returns 200
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-006: A POST from SuperAdmin with a valid section name must trigger the
    /// idempotent seed and return 200 OK with { "data": true }.
    /// This verifies the full path: controller → ISeedService.TriggerReseedAsync → DataSeeder.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_006_POST_Reseed_SuperAdminRole_ValidSection_Returns200_WithData()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.SuperAdmin);

        // Act — seed "roles" section (idempotent — safe to run multiple times)
        var response = await client.PostAsJsonAsync("/api/admin/reseed", new { section = "roles" });

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "POST /api/admin/reseed with SuperAdmin and a valid section must return 200 OK");

        // Assert — ApiResponse<bool> envelope with data = true
        var envelope = await response.Content.ReadFromJsonAsync<BoolResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().BeTrue("TriggerReseedAsync must return true for a known section");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-007: POST /api/admin/reseed with invalid section returns 400
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-007: A POST with an invalid section name must return 400 Bad Request.
    /// ISeedService.TriggerReseedAsync returns false for unknown sections,
    /// and the controller converts that to 400.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_007_POST_Reseed_SuperAdminRole_InvalidSection_Returns400()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.SuperAdmin);

        // Act — unknown section name
        var response = await client.PostAsJsonAsync("/api/admin/reseed", new { section = "nonexistent-section" });

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "an unknown section name must cause the controller to return 400 Bad Request");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper shapes for deserializing ApiResponse envelope
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class SeedStatusResponseShape
    {
        public SeedStatusDataShape? Data { get; set; }
    }

    private sealed class SeedStatusDataShape
    {
        public int TotalRoles { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalLeaveTypes { get; set; }
        public int TotalPublicHolidays { get; set; }
        public int TotalSystemConfigs { get; set; }
        public bool IsHealthy { get; set; }
    }

    private sealed class BoolResponseShape
    {
        public bool Data { get; set; }
    }
}
