// Characterization test — documents current behavior as of commit HEAD, not verified correctness.
// Integration tests for F-01: Authentication & Authorization.
// See IT-F01-001 through IT-F01-006 in docs/test-plan.md.
// If any of these tests start failing after a change, that change altered observable
// authentication/authorization behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-01 — Authentication &amp; Authorization.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters) is real.
///
/// IT- IDs covered: IT-F01-001, IT-F01-002, IT-F01-003, IT-F01-004, IT-F01-005, IT-F01-006.
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class AuthIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    public AuthIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F01-001: Unauthenticated GET /api/employees returns 401 Unauthorized
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F01-001: A request with no Authorization header must be rejected with 401
    /// before it reaches the EmployeeController — the middleware pipeline enforces
    /// the [Authorize] attribute on the controller class.
    /// </summary>
    [Fact]
    public async Task IT_F01_001_Unauthenticated_GET_Employees_Returns401()
    {
        // Arrange — client with no Authorization header.
        // Disable automatic redirect following so we catch the raw status code.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "an unauthenticated request to a protected endpoint must return 401");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F01-002: Valid JWT token allows access to protected endpoints
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F01-002: A request carrying a valid JWT (simulated by TestAuthHandler returning
    /// an authenticated principal) must reach the controller and return a 2xx response.
    /// The response body must include the ApiResponse envelope with a "data" property.
    /// </summary>
    [Fact]
    public async Task IT_F01_002_ValidJwt_AllowsAccessToProtectedEndpoint()
    {
        // Arrange — authenticated as Employee (the minimum role for GET /api/employees).
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert — 200 OK (or any 2xx) proves the request cleared authentication.
        // The in-memory database is empty, so data is an empty paged result.
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "a valid token must allow access to protected endpoints");

        // Assert response.body.data exists (ApiResponse<T> envelope).
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for all 2xx responses");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F01-003: Expired JWT token returns 401
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F01-003: A request with a token that has passed its expiry time must be
    /// rejected with 401. The TestAuthHandler simulates this via the X-Test-Auth-Fail
    /// and X-Test-Fail-Reason headers.
    /// </summary>
    [Fact]
    public async Task IT_F01_003_ExpiredJwt_Returns401()
    {
        // Arrange — signal the TestAuthHandler to simulate an expired token failure.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailReasonHeader, TestAuthScheme.FailReasonExpired);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "an expired JWT must result in 401 — lifetime validation must be enforced");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F01-004: Invalid JWT signature returns 401
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F01-004: A token tampered with an invalid signature must be rejected with 401.
    /// The signing key mismatch is simulated by the TestAuthHandler's fail-reason header.
    /// </summary>
    [Fact]
    public async Task IT_F01_004_InvalidJwtSignature_Returns401()
    {
        // Arrange — signal an invalid-signature failure.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailReasonHeader, TestAuthScheme.FailReasonInvalidSignature);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "a JWT with an invalid signature must result in 401 — signature validation must be enforced");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F01-005: Employee role cannot access HRAdmin-only endpoints (returns 403)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F01-005: A request authenticated as "Employee" must be forbidden (403) when
    /// it targets an endpoint restricted to HRAdmin or SuperAdmin.
    /// POST /api/employees creates an employee profile and requires [Authorize(Roles="HRAdmin,SuperAdmin")].
    /// </summary>
    [Fact]
    public async Task IT_F01_005_EmployeeRole_Cannot_Access_HRAdminOnlyEndpoint_Returns403()
    {
        // Arrange — authenticated as Employee only.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.Employee);

        // Minimal payload — the controller should reject this request at the authorization
        // layer before it reaches any business logic, so the content doesn't matter.
        var payload = new
        {
            employeeCode = "EMP001",
            userId = Guid.NewGuid(),
            departmentId = Guid.NewGuid(),
            jobTitle = "Developer",
            dateOfJoining = DateTime.UtcNow.ToString("O")
        };

        // Act — POST /api/employees requires HRAdmin or SuperAdmin.
        var response = await client.PostAsJsonAsync("/api/employees", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be able to POST to an HRAdmin-only endpoint; " +
            "RBAC must return 403 after authentication succeeds");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F01-006: HRAdmin role can access HR management endpoints (returns 200)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F01-006: A request authenticated as "HRAdmin" must be allowed (200) when
    /// it targets an endpoint restricted to HRAdmin or SuperAdmin.
    /// GET /api/auth/users lists all active users and requires [Authorize(Roles="HRAdmin,SuperAdmin")].
    /// </summary>
    [Fact]
    public async Task IT_F01_006_HRAdminRole_Can_Access_HRManagementEndpoint_Returns200()
    {
        // Arrange — authenticated as HRAdmin.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Act — GET /api/auth/users requires HRAdmin or SuperAdmin.
        var response = await client.GetAsync("/api/auth/users");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "an HRAdmin token must be granted access to HR management endpoints");

        // Assert response.body.data exists (ApiResponse<T> envelope).
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for all 2xx responses");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper — loose shape used only for asserting the ApiResponse envelope
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal POCO for deserializing the <c>{ "data": ... }</c> envelope.
    /// Using <c>object?</c> means the assertion only verifies the property exists
    /// and is non-null, without caring about its exact type.
    /// </summary>
    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }
}
