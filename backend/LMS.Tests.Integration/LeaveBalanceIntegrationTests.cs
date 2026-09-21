// Integration tests for F-05: Leave Balance Management.
// IT- IDs covered: IT-F05-001 through IT-F05-008.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// leave-balance-management behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-05 — Leave Balance Management.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F05-001: GET /api/leave-balances returns 200 with ApiResponse envelope (has "data" key) for HRAdmin
///   IT-F05-002: GET /api/leave-balances/my returns employee's own balances
///   IT-F05-003: GET /api/leave-balances returns 401 without auth
///   IT-F05-004: POST /api/leave-balances/adjust requires HRAdmin role (check 403 for Employee role)
///   IT-F05-005: GET /api/leave-balances/summary returns summary statistics with data key
///   IT-F05-006: GET /api/leave-balances/employee/{id} returns specific employee balances
///   IT-F05-007: POST /api/leave-balances/adjust with invalid data returns 400
///   IT-F05-008: Employee cannot adjust balances (403)
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class LeaveBalanceIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LeaveBalanceIntegrationTests(LmsWebApplicationFactory factory)
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

    /// <summary>
    /// Builds a valid AdjustBalanceDto payload.
    /// </summary>
    private static object BuildAdjustPayload(
        Guid? employeeId = null,
        Guid? leaveTypeId = null,
        int year = 2025,
        decimal adjustment = 1.0m,
        string reason = "Test manual adjustment")
    {
        return new
        {
            employeeId = employeeId ?? Guid.NewGuid(),
            leaveTypeId = leaveTypeId ?? Guid.NewGuid(),
            year,
            adjustment,
            reason
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-001: GET /api/leave-balances returns 200 with { data: [...] } for HRAdmin
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-001: An authenticated GET request to /api/leave-balances as HRAdmin must return 200 OK
    /// and the response body must contain a "data" property (ApiResponse&lt;T&gt; envelope).
    /// The in-memory database may be empty; the property must still be present and non-null.
    /// Covers FR-33 (HR can view all leave balances).
    /// </summary>
    [Fact]
    public async Task IT_F05_001_GET_LeaveBalances_HRAdminRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/leave-balances");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-balances must return 200 OK for authenticated HRAdmin users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-002: GET /api/leave-balances/my returns employee's own balances
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-002: A GET request to /api/leave-balances/my returns the current employee's balances.
    /// The endpoint derives the employee ID from the JWT claim, so any authenticated user may call it.
    /// Covers FR-34 (employees can view own balances).
    /// </summary>
    [Fact]
    public async Task IT_F05_002_GET_LeaveBalancesMy_EmployeeRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/leave-balances/my");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-balances/my must return 200 OK for an authenticated Employee");

        // Assert — envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-003: GET /api/leave-balances returns 401 without auth
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-003: A GET request with no valid token must return 401 Unauthorized.
    /// The [Authorize] class-level attribute must block the request before any business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F05_003_GET_LeaveBalances_Unauthenticated_Returns401()
    {
        // Arrange — client drives TestAuthHandler to simulate auth failure
        var client = CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/leave-balances");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "GET /api/leave-balances without a valid token must return 401 Unauthorized");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-004: POST /api/leave-balances/adjust requires HRAdmin role (403 for Employee)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-004: A POST request to /api/leave-balances/adjust authenticated as Employee
    /// must return 403 Forbidden. Only HRAdmin may adjust balances (FR-38).
    /// </summary>
    [Fact]
    public async Task IT_F05_004_POST_LeaveBalancesAdjust_EmployeeRole_Returns403()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var payload = BuildAdjustPayload();

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-balances/adjust", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "POST /api/leave-balances/adjust with Employee role must return 403 — only HRAdmin may adjust balances (FR-38)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-005: GET /api/leave-balances/summary returns summary statistics with data key
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-005: A GET request to /api/leave-balances/summary by HRAdmin must return 200 OK
    /// with a "data" property containing aggregate summary statistics. Covers FR-40.
    /// </summary>
    [Fact]
    public async Task IT_F05_005_GET_LeaveBalancesSummary_HRAdminRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/leave-balances/summary");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-balances/summary must return 200 OK for HRAdmin");

        // Assert — envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 summary response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-006: GET /api/leave-balances/employee/{id} returns specific employee balances
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-006: A GET request to /api/leave-balances/employee/{id} as Manager or HRAdmin
    /// must return 200 OK with the "data" envelope. The in-memory store may return an empty list;
    /// the envelope must still be present. Covers FR-35.
    /// </summary>
    [Fact]
    public async Task IT_F05_006_GET_LeaveBalancesEmployee_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);
        var employeeId = Guid.NewGuid(); // may not exist; service returns empty collection

        // Act
        var response = await client.GetAsync($"/api/leave-balances/employee/{employeeId}");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-balances/employee/{id} with Manager role must return 200 OK");

        // Assert — envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present even for an unknown employee ID");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-007: POST /api/leave-balances/adjust with invalid data returns 400
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-007: A POST request from HRAdmin with a body that fails model validation
    /// (missing required fields) must return 400 Bad Request before reaching business logic.
    /// </summary>
    [Fact]
    public async Task IT_F05_007_POST_LeaveBalancesAdjust_InvalidData_Returns400()
    {
        // Arrange — omit required fields (EmployeeId, LeaveTypeId, Reason, Year out of range)
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var invalidPayload = new { adjustment = 1.5m }; // missing EmployeeId, LeaveTypeId, Year, Reason

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-balances/adjust", invalidPayload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "POST /api/leave-balances/adjust with missing required fields must return 400 Bad Request");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F05-008: Employee cannot adjust balances (403)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F05-008: A POST request to /api/leave-balances/adjust authenticated as Employee
    /// must return 403 Forbidden. This is the authoritative RBAC check that only HRAdmin
    /// may perform balance adjustments (FR-38).
    /// </summary>
    [Fact]
    public async Task IT_F05_008_POST_LeaveBalancesAdjust_EmployeeCannotAdjust_Returns403()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var payload = BuildAdjustPayload(
            employeeId: Guid.NewGuid(),
            leaveTypeId: Guid.NewGuid(),
            year: 2025,
            adjustment: 2.0m,
            reason: "Unauthorized attempt");

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-balances/adjust", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be authorized to POST to /api/leave-balances/adjust — RBAC must return 403 (FR-38)");
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
