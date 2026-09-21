// Integration tests for F-08: Approval Workflow.
// IT- IDs covered: IT-F08-001 through IT-F08-008.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// approval-workflow behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-08 — Approval Workflow.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F08-001: GET /api/approvals/pending returns 200 with { data: [...] } for Manager role
///   IT-F08-002: GET /api/approvals/pending returns 401 when unauthenticated
///   IT-F08-003: GET /api/approvals/pending returns 403 for Employee role
///   IT-F08-004: GET /api/approvals/history returns 200 with { data: [...] } for Manager role
///   IT-F08-005: GET /api/approvals/stats returns 200 with { data: ... } for Manager role
///   IT-F08-006: POST /api/approvals/{id}/escalate with non-existent ID returns 404 (Manager role)
///   IT-F08-007: Employee role cannot access /api/approvals/pending — returns 403
///   IT-F08-008: All approval read endpoints return a response body with a "data" key
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class ApprovalIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApprovalIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an HttpClient authenticated with the supplied role header value.
    /// </summary>
    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, role);
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated HttpClient.
    /// The TestAuthHandler sees X-Test-Auth-Fail and returns 401.
    /// </summary>
    private HttpClient CreateUnauthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");
        return client;
    }

    // ── IT-F08-001 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-001: GET /api/approvals/pending for a Manager must return 200 OK
    /// and the response body must carry a "data" key (ApiResponse&lt;T&gt; envelope).
    /// The in-memory database may be empty; the "data" property must still be present.
    /// </summary>
    [Fact]
    public async Task IT_F08_001_GET_ApprovalsPending_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/approvals/pending");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/approvals/pending must return 200 OK for an authenticated Manager");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ── IT-F08-002 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-002: A GET request to /api/approvals/pending with no valid token must
    /// return 401 Unauthorized. The [Authorize] attribute on the controller must block
    /// the request before business logic is reached.
    /// </summary>
    [Fact]
    public async Task IT_F08_002_GET_ApprovalsPending_Unauthenticated_Returns401()
    {
        // Arrange
        var client = CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/approvals/pending");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "GET /api/approvals/pending without a valid token must return 401 Unauthorized");
    }

    // ── IT-F08-003 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-003: A GET request to /api/approvals/pending authenticated as Employee must
    /// return 403 Forbidden. Only Manager, HRAdmin, and SuperAdmin are authorised for this
    /// endpoint ([Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]).
    /// </summary>
    [Fact]
    public async Task IT_F08_003_GET_ApprovalsPending_EmployeeRole_Returns403()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/approvals/pending");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "GET /api/approvals/pending with Employee role must return 403 — only Manager/HRAdmin/SuperAdmin are authorised");
    }

    // ── IT-F08-004 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-004: GET /api/approvals/history for a Manager must return 200 OK
    /// and the response body must carry a "data" key (ApiResponse&lt;T&gt; envelope).
    /// </summary>
    [Fact]
    public async Task IT_F08_004_GET_ApprovalsHistory_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/approvals/history");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/approvals/history must return 200 OK for an authenticated Manager");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ── IT-F08-005 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-005: GET /api/approvals/stats for a Manager must return 200 OK
    /// and the response body must carry a "data" key (ApiResponse&lt;ApprovalStatsDto&gt; envelope).
    /// </summary>
    [Fact]
    public async Task IT_F08_005_GET_ApprovalsStats_ManagerRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/approvals/stats");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/approvals/stats must return 200 OK for an authenticated Manager");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present (ApprovalStatsDto) for a 200 response");
    }

    // ── IT-F08-006 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-006: POST /api/approvals/{id}/escalate with a GUID that does not correspond
    /// to any leave request must return 404 Not Found (Manager role).
    /// The service layer throws KeyNotFoundException, which the controller maps to 404.
    /// </summary>
    [Fact]
    public async Task IT_F08_006_POST_ApprovalsEscalate_NonExistentId_ManagerRole_Returns404()
    {
        // Arrange — use a random GUID that was never inserted
        var client = CreateAuthenticatedClient(RoleNames.Manager);
        var nonExistentId = Guid.NewGuid();
        var payload = new { reason = "Escalating to HR for final decision" };

        // Act
        var response = await client.PostAsJsonAsync($"/api/approvals/{nonExistentId}/escalate", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "POST /api/approvals/{id}/escalate for a non-existent leave request ID must return 404 Not Found");
    }

    // ── IT-F08-007 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-007: Employee role must be denied access to /api/approvals/pending with 403.
    /// Verifies that the RBAC guard on the pending endpoint correctly excludes the Employee
    /// role from the approval workflow management surface.
    /// </summary>
    [Fact]
    public async Task IT_F08_007_Employee_CannotAccess_ApprovalPendingList_Returns403()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/approvals/pending");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee must not be able to reach the approval pending list — RBAC must return 403");
    }

    // ── IT-F08-008 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F08-008: Every approval read endpoint must return a JSON body whose top-level
    /// object contains a "data" key, conforming to the project-wide ApiResponse&lt;T&gt; envelope.
    /// Covers /api/approvals/pending, /api/approvals/history, and /api/approvals/stats.
    /// </summary>
    [Fact]
    public async Task IT_F08_008_AllApprovalReadEndpoints_Return_DataKeyInResponseBody()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        var endpoints = new[] { "/api/approvals/pending", "/api/approvals/history", "/api/approvals/stats" };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await client.GetAsync(endpoint);

            // Assert — status is 2xx
            response.IsSuccessStatusCode.Should().BeTrue(
                $"GET {endpoint} with Manager role must return a success status code");

            // Assert — "data" key is present in JSON body
            var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
            envelope.Should().NotBeNull(
                $"GET {endpoint} response must be deserializable as ApiResponse<T>");
            envelope!.Data.Should().NotBeNull(
                $"GET {endpoint} response body must contain a \"data\" key (ApiResponse<T> envelope)");
        }
    }

    // ── Helpers — loose deserialization shapes ─────────────────────────────────

    /// <summary>
    /// Minimal POCO for asserting the <c>{ "data": ... }</c> ApiResponse envelope.
    /// </summary>
    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }
}
