// Integration tests for F-11: Dashboards.
// IT- IDs covered: IT-F11-001 through IT-F11-007.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// dashboard behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-11 — Dashboards.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F11-001: GET /api/dashboards/overview returns 200 with { data: ... } for HRAdmin
///   IT-F11-002: GET /api/dashboards/team-leave returns 200 with { data: ... } for Manager
///   IT-F11-003: GET /api/dashboards/approvals returns 200 with { data: ... } for Manager
///   IT-F11-004: GET /api/dashboards/comp-off returns 200 with { data: ... } for HRAdmin
///   IT-F11-005: GET /api/dashboards/overview returns 401 without auth
///   IT-F11-006: Employee cannot access /api/dashboards/overview (403)
///   IT-F11-007: All dashboard endpoints return JSON with "data" key
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class DashboardIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DashboardIntegrationTests(LmsWebApplicationFactory factory)
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

    private HttpClient CreateUnauthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");
        return client;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-001: GET /api/dashboards/overview returns 200 with "data" key (HRAdmin)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-001: An authenticated GET request to /api/dashboards/overview with HRAdmin role
    /// must return 200 OK and a response body containing a "data" property
    /// (ApiResponse&lt;OverviewDashboardDto&gt; envelope).
    /// </summary>
    [Fact]
    public async Task IT_F11_001_GET_DashboardOverview_HRAdminRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/dashboards/overview");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/dashboards/overview must return 200 OK for authenticated HRAdmin users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 overview response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-002: GET /api/dashboards/team-leave returns 200 with "data" key (Manager)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-002: An authenticated GET request to /api/dashboards/team-leave with Manager role
    /// must return 200 OK and a response body containing a "data" property.
    /// </summary>
    [Fact]
    public async Task IT_F11_002_GET_DashboardTeamLeave_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/dashboards/team-leave");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/dashboards/team-leave must return 200 OK for authenticated Manager users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 team-leave response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-003: GET /api/dashboards/approvals returns 200 with "data" key (Manager)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-003: An authenticated GET request to /api/dashboards/approvals with Manager role
    /// must return 200 OK and a response body containing a "data" property.
    /// </summary>
    [Fact]
    public async Task IT_F11_003_GET_DashboardApprovals_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/dashboards/approvals");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/dashboards/approvals must return 200 OK for authenticated Manager users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 approvals response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-004: GET /api/dashboards/comp-off returns 200 with "data" key (HRAdmin)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-004: An authenticated GET request to /api/dashboards/comp-off with HRAdmin role
    /// must return 200 OK and a response body containing a "data" property.
    /// The endpoint is restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [Fact]
    public async Task IT_F11_004_GET_DashboardCompOff_HRAdminRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/dashboards/comp-off");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/dashboards/comp-off must return 200 OK for authenticated HRAdmin users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 comp-off response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-005: GET /api/dashboards/overview returns 401 without auth
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-005: A GET request to /api/dashboards/overview without a valid token
    /// must return 401 Unauthorized. The controller-level [Authorize] attribute must
    /// block the request before any business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F11_005_GET_DashboardOverview_Unauthenticated_Returns401()
    {
        // Arrange — client drives TestAuthHandler to simulate auth failure
        var client = CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/dashboards/overview");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "GET /api/dashboards/overview without a valid token must return 401 Unauthorized");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-006: Employee cannot access /api/dashboards/overview (403)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-006: A GET request to /api/dashboards/overview authenticated as Employee
    /// must return 403 Forbidden. The endpoint is restricted to HRAdmin and SuperAdmin;
    /// the Employee role must be rejected by the RBAC policy.
    /// </summary>
    [Fact]
    public async Task IT_F11_006_GET_DashboardOverview_EmployeeRole_Returns403()
    {
        // Arrange — Employee role is not in HRAdmin or SuperAdmin
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/dashboards/overview");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be authorized to access /api/dashboards/overview — RBAC must return 403");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F11-007: All dashboard endpoints return JSON with "data" key
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F11-007: Every reachable dashboard endpoint must return a response body
    /// containing a "data" key (ApiResponse&lt;T&gt; envelope contract).
    /// Tested with HRAdmin which has access to all four endpoints.
    /// </summary>
    [Fact]
    public async Task IT_F11_007_AllDashboardEndpoints_ReturnJsonWithDataKey()
    {
        // Arrange — HRAdmin has access to all dashboard endpoints
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        var endpoints = new[]
        {
            "/api/dashboards/overview",
            "/api/dashboards/team-leave",
            "/api/dashboards/approvals",
            "/api/dashboards/comp-off",
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await client.GetAsync(endpoint);

            // Assert — each endpoint must succeed and carry a "data" key
            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                $"GET {endpoint} with HRAdmin role must return 200 OK");

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
            envelope.Should().NotBeNull(
                $"GET {endpoint} response must be wrapped in ApiResponse<T>");
            envelope!.Data.Should().NotBeNull(
                $"GET {endpoint} response body must contain a 'data' key (ApiResponse<T> contract)");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers — loose shapes used for asserting ApiResponse envelope
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal POCO for deserializing the <c>{ "data": ... }</c> envelope
    /// when the inner type is not needed.
    /// </summary>
    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }
}
