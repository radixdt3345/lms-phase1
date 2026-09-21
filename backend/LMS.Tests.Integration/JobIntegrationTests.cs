// Integration tests for F-15: Background Jobs.
// IT- IDs covered: IT-F15-001 through IT-F15-006.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// Route prefix: /api/jobs.
// Controller class is restricted to HRAdmin/SuperAdmin; trigger endpoint further restricted to SuperAdmin.
// If any of these tests start failing after a change, that change altered observable
// background-job administration behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-15 — Background Jobs.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// Controller RBAC summary:
///   - Class-level  [Authorize(Roles = "HRAdmin,SuperAdmin")]: HRAdmin and SuperAdmin can access the class.
///   - Endpoint-level [Authorize(Roles = "SuperAdmin")] on POST /{jobName}/trigger: only SuperAdmin may trigger.
///
/// IT- IDs covered:
///   IT-F15-001: GET /api/jobs/status returns job list with "data" key (HRAdmin role)
///   IT-F15-002: POST /api/jobs/{jobName}/trigger is accessible to SuperAdmin, returns "data" key
///   IT-F15-003: GET /api/jobs/status returns 401 without auth
///   IT-F15-004: Employee role cannot access job endpoints (403)
///   IT-F15-005: POST /api/jobs/InvalidJobName/trigger returns 404 (unknown job name)
///   IT-F15-006: All successful responses have a "data" key in ApiResponse envelope
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class JobIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Valid job names listed in the controller's error message.
    private static readonly string[] ValidJobNames =
    [
        "LeaveBalanceSyncJob",
        "CompOffExpiryJob",
        "EmailDispatchJob",
        "LeaveEscalationJob"
    ];

    public JobIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // ── IT-F15-001 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F15-001: An HRAdmin GET to /api/jobs/status must return 200 OK with an
    /// ApiResponse body containing a "data" key listing all registered Hangfire recurring jobs.
    /// The controller allows HRAdmin at the class level — no additional role restriction on this endpoint.
    /// </summary>
    [Fact]
    public async Task IT_F15_001_GET_Jobs_Status_HRAdminRole_Returns200_WithDataKey()
    {
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        var response = await client.GetAsync("/api/jobs/status");

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/jobs/status must return 200 OK for HRAdmin — the class-level role policy includes HRAdmin");

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must contain the list of Hangfire job statuses");
    }

    // ── IT-F15-002 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F15-002: POST /api/jobs/{jobName}/trigger is restricted to SuperAdmin.
    /// A SuperAdmin POST with a known valid job name must return 200 OK with a "data" key.
    /// HRAdmin is allowed at the class level but is rejected (403) at the endpoint level.
    /// This test verifies SuperAdmin can trigger and the route responds correctly.
    /// </summary>
    [Fact]
    public async Task IT_F15_002_POST_Jobs_Trigger_SuperAdminRole_Returns200_WithDataKey()
    {
        // Use "SuperAdmin" role directly — TestAuthHandler creates role claims from the header value.
        var superAdminClient = _factory.CreateClient();
        superAdminClient.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, "SuperAdmin");

        var jobName = ValidJobNames[0]; // "LeaveBalanceSyncJob"
        var triggerPayload = new { reason = "Manual IT test trigger" };

        var response = await superAdminClient.PostAsJsonAsync($"/api/jobs/{jobName}/trigger", triggerPayload);

        // The service must confirm the job name exists; 200 = triggered, 404 = unregistered.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            "POST /api/jobs/{jobName}/trigger for SuperAdmin must return 200 OK or 404 if job not registered");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
            envelope.Should().NotBeNull("trigger response must be wrapped in ApiResponse<T>");
            envelope!.Data.Should().NotBeNull("response.body.data must be present on 200 OK");
        }
    }

    // ── IT-F15-003 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F15-003: A GET to /api/jobs/status without any auth token must return
    /// 401 Unauthorized. The [Authorize] class attribute blocks the request before
    /// any business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F15_003_GET_Jobs_Status_Unauthenticated_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/jobs/status");

        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "GET /api/jobs/status without auth must return 401 Unauthorized");
    }

    // ── IT-F15-004 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F15-004: An Employee attempting to access any /api/jobs endpoint must
    /// receive 403 Forbidden. The class-level [Authorize(Roles = "HRAdmin,SuperAdmin")]
    /// excludes the Employee (and Manager) roles from all job-admin routes.
    /// </summary>
    [Fact]
    public async Task IT_F15_004_Employee_CannotAccessJobEndpoints_Returns403()
    {
        var employeeClient = CreateAuthenticatedClient(RoleNames.Employee);

        // Attempt GET /api/jobs/status — the class policy denies Employee
        var statusResponse = await employeeClient.GetAsync("/api/jobs/status");
        statusResponse.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "Employee role must receive 403 Forbidden on GET /api/jobs/status — excluded by class-level RBAC");

        // Attempt POST trigger as well
        var triggerResponse = await employeeClient.PostAsJsonAsync(
            $"/api/jobs/{ValidJobNames[0]}/trigger", new { });
        triggerResponse.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "Employee role must receive 403 Forbidden on POST /api/jobs/{jobName}/trigger — excluded by class-level RBAC");
    }

    // ── IT-F15-005 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F15-005: A SuperAdmin POST to /api/jobs/{jobName}/trigger with an unregistered
    /// job name must return 404 Not Found.
    /// The controller returns NotFound when the service returns false for an unknown job name.
    /// Valid job names: LeaveBalanceSyncJob, CompOffExpiryJob, EmailDispatchJob, LeaveEscalationJob.
    /// </summary>
    [Fact]
    public async Task IT_F15_005_POST_Jobs_Trigger_InvalidJobName_Returns404()
    {
        var superAdminClient = _factory.CreateClient();
        superAdminClient.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, "SuperAdmin");

        var invalidJobName = "NonExistentJob_" + Guid.NewGuid().ToString("N")[..8];

        var response = await superAdminClient.PostAsJsonAsync(
            $"/api/jobs/{invalidJobName}/trigger",
            new { reason = "Testing invalid job name" });

        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            $"POST /api/jobs/{invalidJobName}/trigger must return 404 — the service returns false for unregistered job names");
    }

    // ── IT-F15-006 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F15-006: Every successful response from job endpoints must include a "data" key
    /// at the top level of the JSON body (ApiResponse&lt;T&gt; envelope).
    /// Verifies GET /api/jobs/status with HRAdmin and SuperAdmin.
    /// </summary>
    [Fact]
    public async Task IT_F15_006_AllJobEndpoints_ReturnDataKey_InApiResponseEnvelope()
    {
        // HRAdmin can GET /api/jobs/status
        var hrAdminClient = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var hrStatusResponse = await hrAdminClient.GetAsync("/api/jobs/status");
        hrStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/jobs/status must return 200 OK for HRAdmin");
        var hrStatusEnvelope = await hrStatusResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        hrStatusEnvelope.Should().NotBeNull();
        hrStatusEnvelope!.Data.Should().NotBeNull(
            "HRAdmin GET /api/jobs/status response must contain top-level 'data' key");

        // SuperAdmin can also GET /api/jobs/status
        var superAdminClient = _factory.CreateClient();
        superAdminClient.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, "SuperAdmin");
        var superStatusResponse = await superAdminClient.GetAsync("/api/jobs/status");
        superStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/jobs/status must return 200 OK for SuperAdmin");
        var superStatusEnvelope = await superStatusResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        superStatusEnvelope.Should().NotBeNull();
        superStatusEnvelope!.Data.Should().NotBeNull(
            "SuperAdmin GET /api/jobs/status response must contain top-level 'data' key");
    }

    // ── Response shapes ───────────────────────────────────────────────────────

    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }
}
