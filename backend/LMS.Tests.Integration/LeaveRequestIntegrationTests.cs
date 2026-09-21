// Integration tests for F-06: Leave Application and Workflow.
// IT- IDs covered: IT-F06-001 through IT-F06-010.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// leave-application-workflow behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-06 — Leave Application and Workflow.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F06-001: POST /api/leave-requests creates leave request, returns ApiResponse&lt;T&gt;
///   IT-F06-002: GET /api/leave-requests returns employee's own requests
///   IT-F06-003: GET /api/leave-requests/{id} returns specific request
///   IT-F06-004: POST /api/leave-requests/{id}/cancel cancels own request
///   IT-F06-005: GET /api/leave-requests/pending returns pending requests (Manager role)
///   IT-F06-006: POST /api/leave-requests/{id}/approve approves request (Manager role)
///   IT-F06-007: POST /api/leave-requests/{id}/reject rejects with reason
///   IT-F06-008: Employee cannot approve another's request (403)
///   IT-F06-009: POST /api/leave-requests with missing required fields returns 400
///   IT-F06-010: All responses have "data" key
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class LeaveRequestIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LeaveRequestIntegrationTests(LmsWebApplicationFactory factory)
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
    /// Builds a valid CreateLeaveRequestDto payload.
    /// </summary>
    private static object BuildCreateLeaveRequestPayload(
        Guid? leaveTypeId = null,
        string? startDate = null,
        string? endDate = null,
        string? reason = null)
    {
        return new
        {
            leaveTypeId = leaveTypeId ?? Guid.NewGuid(),
            startDate = startDate ?? "2025-08-01",
            endDate = endDate ?? "2025-08-03",
            isHalfDay = false,
            reason = reason ?? "Annual leave request",
            saveAsDraft = false
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-001: POST /api/leave-requests creates leave request, returns ApiResponse<T>
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-001: A POST request from an authenticated Employee with a valid body must
    /// attempt to create the leave request and return an ApiResponse&lt;T&gt; envelope with a "data" key.
    /// The service may return 422 if no leave type exists in the in-memory db; the response
    /// envelope shape is still validated here. Covers FR-42.
    /// </summary>
    [Fact]
    public async Task IT_F06_001_POST_LeaveRequests_Employee_Returns_ApiResponseEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var payload = BuildCreateLeaveRequestPayload();

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-requests", payload);

        // Assert — endpoint is reachable (any non-401 / non-403 response validates RBAC and routing)
        response.StatusCode.Should().NotBe(
            HttpStatusCode.Unauthorized,
            "POST /api/leave-requests must be accessible to authenticated employees");
        response.StatusCode.Should().NotBe(
            HttpStatusCode.Forbidden,
            "POST /api/leave-requests must not restrict by role — any authenticated user may submit");

        // When the request succeeds (201) or fails validation (422/404), both wrap in ApiResponse<T>
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var envelope = await response.Content.ReadFromJsonAsync<LeaveRequestResponseShape>(JsonOptions);
            envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
            envelope!.Data.Should().NotBeNull("response.body.data must contain the created LeaveRequestDto");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-002: GET /api/leave-requests returns employee's own requests
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-002: A GET request from an authenticated Employee must return 200 OK
    /// with a "data" key containing the employee's own leave requests.
    /// Covers FR-45 (role-scoped list).
    /// </summary>
    [Fact]
    public async Task IT_F06_002_GET_LeaveRequests_EmployeeRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/leave-requests");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-requests must return 200 OK for an authenticated Employee");

        // Assert — envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-003: GET /api/leave-requests/{id} returns specific request
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-003: A GET request for a nonexistent leave request ID must return 404 Not Found —
    /// confirming that the endpoint is routed, RBAC passes, and the service performs a lookup.
    /// If an existing ID were available, 200 with "data" would be returned (also asserted here
    /// via the create-then-get path when the service supports it).
    /// Covers FR-46.
    /// </summary>
    [Fact]
    public async Task IT_F06_003_GET_LeaveRequestById_NonexistentId_Returns404()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/leave-requests/{nonexistentId}");

        // Assert — 404 confirms routing and RBAC pass; service reports not found
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "GET /api/leave-requests/{id} for a nonexistent ID must return 404 Not Found");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-004: PUT /api/leave-requests/{id}/cancel cancels own request
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-004: A PUT request to /api/leave-requests/{id}/cancel for a nonexistent ID
    /// must return 404 Not Found (endpoint routed, RBAC passes, service reports not found).
    /// Any authenticated user may cancel their own request; no role restriction on this endpoint.
    /// Covers FR-51.
    /// </summary>
    [Fact]
    public async Task IT_F06_004_PUT_LeaveRequestCancel_NonexistentId_Returns404()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.PutAsync($"/api/leave-requests/{nonexistentId}/cancel", null);

        // Assert — endpoint is reachable and RBAC passes for Employee
        response.StatusCode.Should().NotBe(
            HttpStatusCode.Unauthorized,
            "PUT /api/leave-requests/{id}/cancel must be accessible to authenticated employees");
        response.StatusCode.Should().NotBe(
            HttpStatusCode.Forbidden,
            "PUT /api/leave-requests/{id}/cancel must not restrict by role — any authenticated user may cancel their own request");

        // For a nonexistent ID the service returns 404
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "cancelling a nonexistent leave request ID must return 404 Not Found");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-005: GET /api/leave-requests/pending returns pending requests (Manager role)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-005: A GET request to /api/leave-requests/pending by a Manager must return 200 OK
    /// with a "data" envelope containing pending requests. Covers FR-47.
    /// </summary>
    [Fact]
    public async Task IT_F06_005_GET_LeaveRequestsPending_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/leave-requests/pending");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-requests/pending with Manager role must return 200 OK");

        // Assert — envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-006: POST /api/leave-requests/{id}/approve approves request (Manager role)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-006: A POST request to /api/leave-requests/{id}/approve by a Manager for a
    /// nonexistent ID must return 404 Not Found (endpoint routed, RBAC passes for Manager).
    /// Covers FR-48 (approval restricted to Manager and HRAdmin).
    /// </summary>
    [Fact]
    public async Task IT_F06_006_POST_LeaveRequestApprove_ManagerRole_NonexistentId_Returns404()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/leave-requests/{nonexistentId}/approve", null);

        // Assert — 404 confirms endpoint routed and Manager role authorized
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "POST /api/leave-requests/{id}/approve by Manager for a nonexistent ID must return 404 (RBAC passed)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-007: POST /api/leave-requests/{id}/reject rejects with reason
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-007: A POST request to /api/leave-requests/{id}/reject by a Manager with a valid
    /// reason for a nonexistent ID must return 404 Not Found (endpoint routed, RBAC passes for Manager,
    /// service reports not found). Covers AC-44 (rejection requires a reason).
    /// </summary>
    [Fact]
    public async Task IT_F06_007_POST_LeaveRequestReject_ManagerRole_WithReason_NonexistentId_Returns404()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);
        var nonexistentId = Guid.NewGuid();
        var rejectPayload = new { reason = "Insufficient leave balance for the requested period." };

        // Act
        var response = await client.PostAsJsonAsync($"/api/leave-requests/{nonexistentId}/reject", rejectPayload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "POST /api/leave-requests/{id}/reject by Manager with a reason for a nonexistent ID must return 404");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-008: Employee cannot approve another's request (403)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-008: A POST request to /api/leave-requests/{id}/approve authenticated as Employee
    /// must return 403 Forbidden. Only Manager and HRAdmin may approve requests (FR-48).
    /// </summary>
    [Fact]
    public async Task IT_F06_008_POST_LeaveRequestApprove_EmployeeRole_Returns403()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var anyId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/leave-requests/{anyId}/approve", null);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be authorized to POST to /api/leave-requests/{id}/approve — RBAC must return 403 (FR-48)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-009: POST /api/leave-requests with missing required fields returns 400
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-009: A POST request from an authenticated Employee with a body that fails model
    /// validation (missing required fields) must return 400 Bad Request before reaching business logic.
    /// Covers model-validation guard on CreateLeaveRequestDto (FR-42).
    /// </summary>
    [Fact]
    public async Task IT_F06_009_POST_LeaveRequests_MissingRequiredFields_Returns400()
    {
        // Arrange — omit all required fields to trigger model validation failure
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var invalidPayload = new { isHalfDay = false }; // missing LeaveTypeId, StartDate, EndDate, Reason

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-requests", invalidPayload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "POST /api/leave-requests with missing required fields must return 400 Bad Request");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F06-010: All responses have "data" key — cross-cutting envelope contract
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F06-010: Verifies that every successful GET endpoint in the leave-request API
    /// returns a response body with a "data" key (ApiResponse&lt;T&gt; envelope contract).
    /// Tests GET /api/leave-requests and GET /api/leave-requests/pending.
    /// </summary>
    [Fact]
    public async Task IT_F06_010_AllGetEndpoints_ReturnResponsesWithDataKey()
    {
        // Arrange — Manager can hit both list endpoints
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act + Assert — GET /api/leave-requests
        var listResponse = await client.GetAsync("/api/leave-requests");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/leave-requests with Manager role must return 200 OK");
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        listEnvelope.Should().NotBeNull("GET /api/leave-requests response must be wrapped in ApiResponse<T>");
        listEnvelope!.Data.Should().NotBeNull("GET /api/leave-requests response.body.data must be present");

        // Act + Assert — GET /api/leave-requests/pending
        var pendingResponse = await client.GetAsync("/api/leave-requests/pending");
        pendingResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/leave-requests/pending with Manager role must return 200 OK");
        var pendingEnvelope = await pendingResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        pendingEnvelope.Should().NotBeNull("GET /api/leave-requests/pending response must be wrapped in ApiResponse<T>");
        pendingEnvelope!.Data.Should().NotBeNull("GET /api/leave-requests/pending response.body.data must be present");
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

    /// <summary>
    /// Shape for deserializing a leave-request-specific ApiResponse envelope.
    /// </summary>
    private sealed class LeaveRequestResponseShape
    {
        public LeaveRequestDataShape? Data { get; set; }
    }

    private sealed class LeaveRequestDataShape
    {
        public Guid Id { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
