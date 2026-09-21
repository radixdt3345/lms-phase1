// Integration tests for F-12: Reports & CSV Export.
// IT- IDs covered: IT-F12-001 through IT-F12-007.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// report behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-12 — Reports &amp; CSV Export.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F12-001: POST /api/reports/request creates a report job, returns ApiResponse&lt;T&gt; with "data" key
///   IT-F12-002: GET /api/reports returns user's report jobs with "data" key
///   IT-F12-003: GET /api/reports/{id} returns specific job with "data" key
///   IT-F12-004: GET /api/reports/{id}/download for non-existent job returns 404
///   IT-F12-005: POST /api/reports/request returns 401 without auth
///   IT-F12-006: POST /api/reports/request with invalid report type returns 400
///   IT-F12-007: All JSON report endpoints have "data" key in response body
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class ReportIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportIntegrationTests(LmsWebApplicationFactory factory)
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

    private static object BuildReportRequestPayload(string reportType = "LeaveBalanceSummary")
    {
        return new
        {
            reportType,
            startDate    = "2024-01-01",
            endDate      = "2024-12-31",
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-001: POST /api/reports/request creates a report job, returns ApiResponse<T> with "data" key
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-001: A POST request from an authenticated user to /api/reports/request with
    /// a valid report type must create a report job and return 200 OK with an ApiResponse&lt;T&gt;
    /// envelope whose "data" property contains the new ReportJobDto.
    /// </summary>
    [Fact]
    public async Task IT_F12_001_POST_ReportRequest_AuthenticatedUser_CreatesJob_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var payload = BuildReportRequestPayload("LeaveBalanceSummary");

        // Act
        var response = await client.PostAsJsonAsync("/api/reports/request", payload);

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "POST /api/reports/request with a valid payload must return 200 OK");

        // Assert — ApiResponse<T> envelope has "data" property containing the job
        var envelope = await response.Content.ReadFromJsonAsync<ReportJobResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the created ReportJobDto");
        envelope.Data!.Id.Should().NotBeEmpty("the created report job must have an assigned ID");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-002: GET /api/reports returns user's report jobs with "data" key
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-002: A GET request to /api/reports from an authenticated user must return 200 OK
    /// with an ApiResponse&lt;T&gt; envelope whose "data" property lists that user's report jobs.
    /// The list may be empty; the "data" key must still be present.
    /// </summary>
    [Fact]
    public async Task IT_F12_002_GET_Reports_AuthenticatedUser_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/reports");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/reports must return 200 OK for an authenticated user");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present (may be an empty array) for a 200 list response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-003: GET /api/reports/{id} returns specific job with "data" key
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-003: A GET request to /api/reports/{id} for an existing report job must return
    /// 200 OK with an ApiResponse&lt;T&gt; envelope whose "data" property contains the ReportJobDto
    /// matching the requested ID.
    /// </summary>
    [Fact]
    public async Task IT_F12_003_GET_ReportById_ExistingId_Returns200_WithDataEnvelope()
    {
        // Arrange — create a job first so there is something to fetch
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var createResponse = await client.PostAsJsonAsync("/api/reports/request", BuildReportRequestPayload());
        createResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "pre-condition: creating a report job must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ReportJobResponseShape>(JsonOptions);
        createEnvelope!.Data.Should().NotBeNull("pre-condition: created job must have a data body");
        var createdId = createEnvelope.Data!.Id;

        // Act
        var response = await client.GetAsync($"/api/reports/{createdId}");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/reports/{id} for an existing report job must return 200 OK");

        // Assert — ApiResponse<T> envelope has "data" property matching the created job
        var envelope = await response.Content.ReadFromJsonAsync<ReportJobResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the ReportJobDto");
        envelope.Data!.Id.Should().Be(createdId, "the returned job ID must match the requested ID");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-004: GET /api/reports/{id}/download for non-existent job returns 404
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-004: A GET request to /api/reports/{id}/download for a GUID that was never
    /// created must return 404 Not Found. The service layer must throw KeyNotFoundException,
    /// which the controller maps to a 404 ProblemDetails response.
    /// </summary>
    [Fact]
    public async Task IT_F12_004_GET_ReportDownload_NonexistentId_Returns404()
    {
        // Arrange — use a random GUID that was never inserted
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/reports/{nonexistentId}/download");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "GET /api/reports/{id}/download for a nonexistent job ID must return 404 Not Found");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-005: POST /api/reports/request returns 401 without auth
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-005: A POST request to /api/reports/request without a valid token must return
    /// 401 Unauthorized. The controller-level [Authorize] attribute must block the request
    /// before any business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F12_005_POST_ReportRequest_Unauthenticated_Returns401()
    {
        // Arrange — client drives TestAuthHandler to simulate auth failure
        var client = CreateUnauthenticatedClient();
        var payload = BuildReportRequestPayload();

        // Act
        var response = await client.PostAsJsonAsync("/api/reports/request", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "POST /api/reports/request without a valid token must return 401 Unauthorized");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-006: POST /api/reports/request with invalid report type returns 400
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-006: A POST request from an authenticated user with an invalid or empty
    /// report type must return 400 Bad Request. Model validation or service-layer
    /// validation must reject the request before persisting anything.
    /// </summary>
    [Fact]
    public async Task IT_F12_006_POST_ReportRequest_InvalidReportType_Returns400()
    {
        // Arrange — supply an empty reportType to fail validation
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var invalidPayload = new { reportType = string.Empty };

        // Act
        var response = await client.PostAsJsonAsync("/api/reports/request", invalidPayload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "POST /api/reports/request with an empty or invalid reportType must return 400 Bad Request");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F12-007: All JSON report endpoints have "data" key in response body
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F12-007: Every JSON-returning report endpoint must include a "data" key in
    /// the response body (ApiResponse&lt;T&gt; envelope contract). Tested end-to-end:
    /// create a job, list jobs, get by ID — each must carry "data".
    /// The download endpoint is excluded as it returns text/csv, not JSON.
    /// </summary>
    [Fact]
    public async Task IT_F12_007_AllJsonReportEndpoints_HaveDataKeyInResponseBody()
    {
        // Arrange — create a report job so the list and get-by-id endpoints have data
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var createResponse = await client.PostAsJsonAsync("/api/reports/request", BuildReportRequestPayload());
        createResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "pre-condition: creating a report job must succeed (200 OK)");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ReportJobResponseShape>(JsonOptions);
        createEnvelope!.Data.Should().NotBeNull("pre-condition: the created job envelope must carry 'data'");
        var jobId = createEnvelope.Data!.Id;

        // Assert — POST /api/reports/request response has "data"
        createEnvelope.Data.Should().NotBeNull(
            "POST /api/reports/request response body must contain a 'data' key (ApiResponse<T> contract)");

        // Act & Assert — GET /api/reports has "data"
        var listResponse = await client.GetAsync("/api/reports");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/reports must return 200 OK for an authenticated user");
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        listEnvelope!.Data.Should().NotBeNull(
            "GET /api/reports response body must contain a 'data' key (ApiResponse<T> contract)");

        // Act & Assert — GET /api/reports/{id} has "data"
        var getResponse = await client.GetAsync($"/api/reports/{jobId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/reports/{id} must return 200 OK for an existing job");
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ReportJobResponseShape>(JsonOptions);
        getEnvelope!.Data.Should().NotBeNull(
            "GET /api/reports/{id} response body must contain a 'data' key (ApiResponse<T> contract)");
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
    /// Shape for deserializing a report-job-specific ApiResponse envelope.
    /// Only fields referenced by assertions are declared.
    /// </summary>
    private sealed class ReportJobResponseShape
    {
        public ReportJobDataShape? Data { get; set; }
    }

    private sealed class ReportJobDataShape
    {
        public Guid Id { get; set; }
        public string? ReportType { get; set; }
        public string? Status { get; set; }
        public string? RequestedBy { get; set; }
    }
}
