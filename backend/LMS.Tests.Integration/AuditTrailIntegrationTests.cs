// Integration tests for F-13: Audit Trail.
// See IT-F13-001 through IT-F13-006 in docs/test-plan.md.
// All tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory DB → response.
// JWT validation is replaced by TestAuthHandler; real routing, authorization, and EF Core are active.

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-13 — Audit Trail.
/// Covers: GET /api/audit-logs with auth checks, filters (actionType, recordType, date range),
/// and pagination.
///
/// IT- IDs covered: IT-F13-001, IT-F13-002, IT-F13-003, IT-F13-004, IT-F13-005, IT-F13-006.
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class AuditTrailIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    // Fixed actor ID used across seed data for filter tests
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public AuditTrailIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Seeding helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds audit log entries into an isolated in-memory DB via a scoped service scope,
    /// then returns an HttpClient wired to the same factory instance.
    /// Each call creates a fresh factory clone so seeds don't bleed across tests.
    /// </summary>
    private (LmsWebApplicationFactory Factory, HttpClient Client) CreateIsolatedClient(
        IEnumerable<AuditLog> seedEntries)
    {
        // Create a new factory instance per test so the in-memory DB is isolated.
        var factory = new LmsWebApplicationFactory();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LmsDbContext>();

        db.AuditLogs.AddRange(seedEntries);
        db.SaveChanges();

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return (factory, client);
    }

    private static AuditLog MakeLog(
        string actionType,
        string recordType,
        DateTimeOffset? timestamp = null,
        Guid? actorUserId = null) => new()
    {
        Id = Guid.NewGuid(),
        ActorUserId = actorUserId ?? ActorId,
        ActorEmail = "actor@lms-test.onmicrosoft.com",
        ActionType = actionType,
        RecordType = recordType,
        RecordId = Guid.NewGuid().ToString(),
        OldValue = null,
        NewValue = "{\"name\":\"test\"}",
        IpAddress = "127.0.0.1",
        Timestamp = timestamp ?? DateTimeOffset.UtcNow,
    };

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F13-001: Unauthenticated request returns 401
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F13-001: GET /api/audit-logs with no Authorization header must return 401 Unauthorized.
    /// The [Authorize(Policy = "HRAdminOrSuperAdmin")] attribute on AuditLogController must
    /// reject unauthenticated callers before the action method is reached.
    /// </summary>
    [Fact]
    public async Task IT_F13_001_Unauthenticated_GET_AuditLogs_Returns401()
    {
        // Arrange — no auth header; TestAuthHandler sees X-Test-Auth-Fail and returns Fail.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");

        // Act
        var response = await client.GetAsync("/api/audit-logs");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "unauthenticated requests to /api/audit-logs must be rejected with 401");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F13-002: HRAdmin returns 200 with ApiResponse<AuditLogPagedResultDto> envelope
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F13-002: GET /api/audit-logs authenticated as HRAdmin must return 200 OK.
    /// The response body must be an ApiResponse envelope containing a { data: { items: [...] } }
    /// structure — AuditLogPagedResultDto wrapped in ApiResponse&lt;T&gt;.
    /// </summary>
    [Fact]
    public async Task IT_F13_002_HRAdmin_GET_AuditLogs_Returns200_WithDataEnvelope()
    {
        // Arrange — seed two audit log entries into an isolated DB.
        var (factory, client) = CreateIsolatedClient(new[]
        {
            MakeLog("Create", "Employee"),
            MakeLog("Update", "Department"),
        });
        await using var _ = factory;

        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/audit-logs");

        // Assert status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "an HRAdmin token must be granted access to GET /api/audit-logs");

        // Assert envelope: { data: { items: [...], totalCount, page, pageSize } }
        var envelope = await response.Content.ReadFromJsonAsync<AuditLogEnvelope>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for all 2xx responses");
        envelope.Data!.Items.Should().NotBeNull("data.items must be present in the paged result");
        envelope.Data.TotalCount.Should().Be(2, "both seeded entries must be returned");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F13-003: actionType=Create filter returns only Create entries
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F13-003: GET /api/audit-logs?actionType=Create must return only audit log entries
    /// whose ActionType is "Create" (case-insensitive). Entries with other action types must
    /// be excluded from the result set.
    /// </summary>
    [Fact]
    public async Task IT_F13_003_ActionTypeFilter_Create_ReturnsOnlyCreateEntries()
    {
        // Arrange — seed one Create and one Update entry.
        var (factory, client) = CreateIsolatedClient(new[]
        {
            MakeLog("Create", "Employee"),
            MakeLog("Update", "Employee"),
            MakeLog("Delete", "Department"),
        });
        await using var _ = factory;

        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/audit-logs?actionType=Create");

        // Assert status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert envelope and filtered items
        var envelope = await response.Content.ReadFromJsonAsync<AuditLogEnvelope>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Items.Should().NotBeNull();

        var items = envelope.Data.Items.ToList();
        items.Should().HaveCount(1, "only the Create entry must be returned when actionType=Create");
        items.Should().OnlyContain(
            x => x.ActionType.Equals("Create", StringComparison.OrdinalIgnoreCase),
            "every returned item must have ActionType 'Create'");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F13-004: recordType=Department filter returns only Department entries
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F13-004: GET /api/audit-logs?recordType=Department must return only audit log entries
    /// whose RecordType is "Department" (case-insensitive). Employee and other record types
    /// must be excluded from the result set.
    /// </summary>
    [Fact]
    public async Task IT_F13_004_RecordTypeFilter_Department_ReturnsOnlyDepartmentEntries()
    {
        // Arrange — seed Department and Employee entries.
        var (factory, client) = CreateIsolatedClient(new[]
        {
            MakeLog("Create", "Department"),
            MakeLog("Update", "Department"),
            MakeLog("Create", "Employee"),
            MakeLog("Delete", "LeaveType"),
        });
        await using var _ = factory;

        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/audit-logs?recordType=Department");

        // Assert status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert only Department entries are returned
        var envelope = await response.Content.ReadFromJsonAsync<AuditLogEnvelope>();
        envelope!.Data.Should().NotBeNull();

        var items = envelope.Data!.Items.ToList();
        items.Should().HaveCount(2, "only the two Department entries must be returned when recordType=Department");
        items.Should().OnlyContain(
            x => x.RecordType.Equals("Department", StringComparison.OrdinalIgnoreCase),
            "every returned item must have RecordType 'Department'");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F13-005: dateFrom / dateTo filter returns only entries in the date range
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F13-005: GET /api/audit-logs?dateFrom=X&amp;dateTo=Y must return only entries whose
    /// Timestamp falls within the inclusive range [dateFrom, dateTo]. Entries outside the range
    /// must be excluded from the result set.
    /// </summary>
    [Fact]
    public async Task IT_F13_005_DateRange_Filter_ReturnsOnlyEntriesInRange()
    {
        // Arrange — seed entries at known timestamps spread across multiple days.
        var baseDate = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var (factory, client) = CreateIsolatedClient(new[]
        {
            MakeLog("Create", "Employee", timestamp: baseDate.AddDays(-2)),  // before range — excluded
            MakeLog("Update", "Employee", timestamp: baseDate),              // in range — included
            MakeLog("Delete", "Department", timestamp: baseDate.AddDays(1)), // in range — included
            MakeLog("Create", "LeaveType", timestamp: baseDate.AddDays(5)),  // after range — excluded
        });
        await using var _ = factory;

        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        var dateFrom = baseDate.AddDays(-1).ToString("o");
        var dateTo   = baseDate.AddDays(2).ToString("o");

        // Act
        var response = await client.GetAsync($"/api/audit-logs?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}");

        // Assert status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert only in-range entries are returned
        var envelope = await response.Content.ReadFromJsonAsync<AuditLogEnvelope>();
        envelope!.Data.Should().NotBeNull();

        var items = envelope.Data!.Items.ToList();
        items.Should().HaveCount(2, "only entries within the [dateFrom, dateTo] range must be returned");

        var rangeStart = baseDate.AddDays(-1);
        var rangeEnd   = baseDate.AddDays(2).AddDays(1); // service adds 1 day when no time component
        items.Should().OnlyContain(
            x => x.Timestamp >= rangeStart && x.Timestamp < rangeEnd,
            "every returned item must have a Timestamp within the requested range");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F13-006: Pagination — pageNumber=1&pageSize=5 returns the correct page
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F13-006: GET /api/audit-logs?page=1&amp;pageSize=5 must return the first 5 entries
    /// (ordered by Timestamp descending) and report the correct totalCount, page, and pageSize
    /// values in the paged result DTO.
    /// </summary>
    [Fact]
    public async Task IT_F13_006_Pagination_Page1_PageSize5_ReturnsCorrectPage()
    {
        // Arrange — seed 10 entries so that pagination is exercised.
        var baseDate = new DateTimeOffset(2025, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var entries = Enumerable.Range(0, 10)
            .Select(i => MakeLog("Create", "Employee", timestamp: baseDate.AddHours(i)))
            .ToList();

        var (factory, client) = CreateIsolatedClient(entries);
        await using var _ = factory;

        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/audit-logs?page=1&pageSize=5");

        // Assert status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert pagination metadata and item count
        var envelope = await response.Content.ReadFromJsonAsync<AuditLogEnvelope>();
        envelope!.Data.Should().NotBeNull("response.body.data must be present");

        var paged = envelope.Data!;
        paged.TotalCount.Should().Be(10, "all 10 seeded entries must be counted");
        paged.Page.Should().Be(1, "requested page 1 must be echoed back");
        paged.PageSize.Should().Be(5, "requested pageSize 5 must be echoed back");
        paged.Items.Should().HaveCount(5, "only the first 5 entries must appear on page 1");

        // Entries are returned newest-first (OrderByDescending Timestamp)
        var itemList = paged.Items.ToList();
        itemList.Should().BeInDescendingOrder(x => x.Timestamp,
            "GET /api/audit-logs orders results by Timestamp descending");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Response shape POCOs — used for deserializing the ApiResponse<T> envelope
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class AuditLogEnvelope
    {
        public AuditLogPagedResultShape? Data { get; set; }
    }

    private sealed class AuditLogPagedResultShape
    {
        public IEnumerable<AuditLogItemShape> Items { get; set; } = Enumerable.Empty<AuditLogItemShape>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AuditLogItemShape
    {
        public Guid Id { get; set; }
        public Guid ActorUserId { get; set; }
        public string ActorEmail { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}
