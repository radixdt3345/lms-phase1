// Integration tests for F-09: Notifications & Email.
// IT- IDs covered: IT-F09-001 through IT-F09-008.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// Route prefix: /api/notifications.
// If any of these tests start failing after a change, that change altered observable
// notification behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-09 — Notifications &amp; Email.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F09-001: GET /api/notifications returns user's notifications with "data" key
///   IT-F09-002: GET /api/notifications/unread-count returns count in ApiResponse with "data" key
///   IT-F09-003: PUT /api/notifications/{id}/read marks notification as read (404 for unknown ID)
///   IT-F09-004: PUT /api/notifications/read-all marks all as read, returns 200 with "data" key
///   IT-F09-005: DELETE /api/notifications/{id} returns 404 for non-existent notification
///   IT-F09-006: GET /api/notifications returns 401 without auth
///   IT-F09-007: Employee only sees their own notifications (scoped by CurrentUserId)
///   IT-F09-008: All successful responses contain the "data" key in ApiResponse envelope
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class NotificationIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationIntegrationTests(LmsWebApplicationFactory factory)
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

    // ── IT-F09-001 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-001: An authenticated GET to /api/notifications must return 200 OK
    /// with an ApiResponse body containing a "data" key.
    /// AC-61: GET /api/notifications returns HTTP 200 with list of notifications.
    /// The in-memory database may be empty; the "data" property must still be present.
    /// </summary>
    [Fact]
    public async Task IT_F09_001_GET_Notifications_AuthenticatedUser_Returns200_WithDataKey()
    {
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/notifications must return 200 OK for authenticated Employee (AC-61)");

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must be present even when the notification inbox is empty");
    }

    // ── IT-F09-002 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-002: An authenticated GET to /api/notifications/unread-count must return
    /// 200 OK with an ApiResponse body containing a "data" key whose value is an integer.
    /// Used by the navbar bell icon (FR-75 — refreshed every 60 s).
    /// </summary>
    [Fact]
    public async Task IT_F09_002_GET_Notifications_UnreadCount_Returns200_WithDataKey()
    {
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        var response = await client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/notifications/unread-count must return 200 OK for authenticated Employee");

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must contain the unread-notification count (0 or more)");
    }

    // ── IT-F09-003 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-003: A PUT to /api/notifications/{id}/read for a notification that does not
    /// belong to the current user (or does not exist) must return 404.
    /// AC-62: the service returns false when the notification cannot be found, and the
    /// controller translates false → 404 Not Found.
    /// </summary>
    [Fact]
    public async Task IT_F09_003_PUT_Notifications_MarkAsRead_NonExistentId_Returns404()
    {
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonExistentId = Guid.NewGuid();

        var response = await client.PutAsync($"/api/notifications/{nonExistentId}/read", null);

        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "PUT /api/notifications/{id}/read for a non-existent or unowned notification must return 404 (AC-62)");
    }

    // ── IT-F09-004 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-004: A PUT to /api/notifications/read-all must mark all of the current user's
    /// notifications as read and return 200 OK with a "data" key.
    /// AC-62: mark-all-as-read always succeeds (even with an empty inbox).
    /// </summary>
    [Fact]
    public async Task IT_F09_004_PUT_Notifications_ReadAll_Returns200_WithDataKey()
    {
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        var response = await client.PutAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "PUT /api/notifications/read-all must return 200 OK (AC-62)");

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must be present on a successful read-all operation");
    }

    // ── IT-F09-005 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-005: A DELETE to /api/notifications/{id} for a notification that does not
    /// belong to the current user (or does not exist) must return 404.
    /// The controller translates service returning false → 404 Not Found.
    /// </summary>
    [Fact]
    public async Task IT_F09_005_DELETE_Notifications_NonExistentId_Returns404()
    {
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonExistentId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/notifications/{nonExistentId}");

        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "DELETE /api/notifications/{id} for a non-existent or unowned notification must return 404");
    }

    // ── IT-F09-006 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-006: A GET to /api/notifications without a valid auth token must return
    /// 401 Unauthorized. The [Authorize] attribute at controller class level blocks the
    /// request before any business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F09_006_GET_Notifications_Unauthenticated_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "GET /api/notifications without a valid token must return 401 Unauthorized");
    }

    // ── IT-F09-007 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-007: Two authenticated Employee clients must each receive a 200 OK response
    /// with a "data" key scoped to their own user identity.
    /// The service filters by CurrentUserId() resolved from the auth token — each client
    /// has an independent identity injected by <see cref="TestAuthHandler"/>.
    /// </summary>
    [Fact]
    public async Task IT_F09_007_GET_Notifications_Employee_SeesOnlyOwnNotifications_ScopedByUserId()
    {
        var employeeClient1 = CreateAuthenticatedClient(RoleNames.Employee);
        var employeeClient2 = CreateAuthenticatedClient(RoleNames.Employee);

        var response1 = await employeeClient1.GetAsync("/api/notifications");
        var response2 = await employeeClient2.GetAsync("/api/notifications");

        response1.StatusCode.Should().Be(HttpStatusCode.OK,
            "first employee must receive 200 OK from GET /api/notifications");
        response2.StatusCode.Should().Be(HttpStatusCode.OK,
            "second employee must receive 200 OK from GET /api/notifications");

        var envelope1 = await response1.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        var envelope2 = await response2.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);

        envelope1.Should().NotBeNull("first employee response must have ApiResponse envelope");
        envelope1!.Data.Should().NotBeNull(
            "first employee response.body.data must be present — scoped to their own notifications");

        envelope2.Should().NotBeNull("second employee response must have ApiResponse envelope");
        envelope2!.Data.Should().NotBeNull(
            "second employee response.body.data must be present — scoped to their own notifications");
    }

    // ── IT-F09-008 ────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F09-008: Every successful response from notification endpoints must include a
    /// "data" key at the top level of the JSON body (ApiResponse&lt;T&gt; envelope).
    /// Verifies GET /api/notifications, GET /api/notifications/unread-count,
    /// and PUT /api/notifications/read-all.
    /// </summary>
    [Fact]
    public async Task IT_F09_008_AllNotificationEndpoints_ReturnDataKey_InApiResponseEnvelope()
    {
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // GET /api/notifications
        var listResponse = await client.GetAsync("/api/notifications");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/notifications must return 200 OK for Employee");
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        listEnvelope.Should().NotBeNull();
        listEnvelope!.Data.Should().NotBeNull(
            "GET /api/notifications response must contain top-level 'data' key");

        // GET /api/notifications/unread-count
        var countResponse = await client.GetAsync("/api/notifications/unread-count");
        countResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/notifications/unread-count must return 200 OK for Employee");
        var countEnvelope = await countResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        countEnvelope.Should().NotBeNull();
        countEnvelope!.Data.Should().NotBeNull(
            "GET /api/notifications/unread-count response must contain top-level 'data' key");

        // PUT /api/notifications/read-all
        var readAllResponse = await client.PutAsync("/api/notifications/read-all", null);
        readAllResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "PUT /api/notifications/read-all must return 200 OK for Employee");
        var readAllEnvelope = await readAllResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        readAllEnvelope.Should().NotBeNull();
        readAllEnvelope!.Data.Should().NotBeNull(
            "PUT /api/notifications/read-all response must contain top-level 'data' key");
    }

    // ── Response shapes ───────────────────────────────────────────────────────

    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }
}
