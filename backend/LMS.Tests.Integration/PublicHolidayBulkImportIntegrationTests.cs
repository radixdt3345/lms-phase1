// Integration tests for F-10: Public Holiday Management — Bulk Import (AC-63).
// Tests cover IT-F10-INT-009 through IT-F10-INT-013 from docs/test-plan.md.
// These exercise the full ASP.NET Core pipeline for the POST /api/public-holidays/bulk endpoint:
//   middleware → controller → IPublicHolidayService.BulkImportAsync → in-memory database.
//
// AC-63: POST /api/public-holidays/bulk with a valid JSON list returns HTTP 200
//         with the list of created holiday records wrapped in { "data": [...] }.
//         Duplicate Name+Date pairs in subsequent imports are silently skipped.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-10 — Public Holiday Bulk Import (AC-63).
/// Tests exercise POST /api/public-holidays/bulk through the full ASP.NET Core pipeline.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, service, in-memory EF Core) is real.
///
/// IT- IDs covered: IT-F10-INT-009 through IT-F10-INT-013.
/// </summary>
public class PublicHolidayBulkImportIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PublicHolidayBulkImportIntegrationTests(LmsWebApplicationFactory factory)
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

    private static object[] BuildBulkPayload(string suffix) =>
    [
        new { name = $"IT009 Holiday A {suffix}", date = "2027-01-26", countryCode = "IN", isOptional = false },
        new { name = $"IT009 Holiday B {suffix}", date = "2027-08-15", countryCode = "IN", isOptional = true, description = "Independence Day test" },
        new { name = $"IT009 Holiday C {suffix}", date = "2027-10-02", countryCode = "IN", isOptional = false }
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-009: POST /api/public-holidays/bulk without auth returns 401
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-009: An unauthenticated POST to /api/public-holidays/bulk must return 401.
    /// The [Authorize] attribute on the controller must deny this request.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_009_POST_BulkImport_NoAuth_Returns401()
    {
        // Arrange — no auth; TestAuthHandler returns Fail when X-Test-Auth-Fail is set
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");

        var payload = BuildBulkPayload(Guid.NewGuid().ToString("N")[..6]);

        // Act
        var response = await client.PostAsJsonAsync("/api/public-holidays/bulk", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "an unauthenticated request to POST /api/public-holidays/bulk must return 401");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-010: POST /api/public-holidays/bulk with Employee role returns 403
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-010: A POST authenticated as Employee must be rejected with 403 Forbidden.
    /// The [Authorize(Roles = "HRAdmin,SuperAdmin")] on BulkImport must deny Employee requests.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_010_POST_BulkImport_EmployeeRole_Returns403()
    {
        // Arrange — Employee is not HRAdmin or SuperAdmin
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var payload = BuildBulkPayload(Guid.NewGuid().ToString("N")[..6]);

        // Act
        var response = await client.PostAsJsonAsync("/api/public-holidays/bulk", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee must not be authorized to bulk-import holidays; RBAC must return 403");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-011: POST /api/public-holidays/bulk with HRAdmin and valid list returns 200
    //                 with { data: [...] } envelope (AC-63)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-011: A POST from HRAdmin with a valid JSON list of holiday DTOs must return
    /// 200 OK, and the body must contain a "data" array (ApiResponse&lt;T&gt; envelope).
    /// This is the primary assertion for AC-63.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_011_POST_BulkImport_HRAdminRole_ValidList_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var payload = BuildBulkPayload(suffix);

        // Act
        var response = await client.PostAsJsonAsync("/api/public-holidays/bulk", payload);

        // Assert — status (AC-63: endpoint must return success)
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "POST /api/public-holidays/bulk with HRAdmin and a valid list must return 200 OK");

        // Assert — ApiResponse<T> envelope has "data" property (frontend reads response.data.data)
        var envelope = await response.Content.ReadFromJsonAsync<BulkImportResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the list of created holidays");

        // Assert — returned list has the correct number of created holidays
        envelope.Data!.Should().HaveCount(3,
            "bulk import of 3 distinct holidays must return all 3 created records");

        // Assert — each returned holiday has an Id (record was persisted)
        envelope.Data.Should().AllSatisfy(h =>
            h.Id.Should().NotBe(Guid.Empty, "each bulk-imported holiday must have a server-generated Id"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-012: Bulk-imported holidays are retrievable via GET?year=
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-012: Holidays created via POST /api/public-holidays/bulk must be persisted
    /// and retrievable via GET /api/public-holidays?year={year}.
    /// This verifies the full write-read round-trip required by AC-63.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_012_POST_BulkImport_CreatedHolidays_RetrievableViaGet()
    {
        // Arrange — unique suffix prevents collision with other tests in shared factory
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        const int targetYear = 2028;

        var payload = new[]
        {
            new { name = $"BulkRead Holiday A {suffix}", date = $"{targetYear}-03-17", countryCode = "IN", isOptional = false },
            new { name = $"BulkRead Holiday B {suffix}", date = $"{targetYear}-11-14", countryCode = "IN", isOptional = true }
        };

        // Act — bulk import
        var importResponse = await client.PostAsJsonAsync("/api/public-holidays/bulk", payload);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK, "pre-condition: bulk import must succeed");

        // Assert — ApiResponse data exists
        var importEnvelope = await importResponse.Content.ReadFromJsonAsync<BulkImportResponseShape>(JsonOptions);
        importEnvelope!.Data.Should().NotBeNull("response.body.data must be present");

        // Act — retrieve by year
        var getResponse = await client.GetAsync($"/api/public-holidays?year={targetYear}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, "GET by year must return 200 OK");

        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiListResponseShape>(JsonOptions);
        getEnvelope!.Data.Should().NotBeNull("response.body.data must contain the list");

        // Assert — both imported holidays appear in the GET response
        var returnedNames = getEnvelope.Data!.Select(h => h.Name).ToList();
        returnedNames.Should().Contain($"BulkRead Holiday A {suffix}",
            "the first bulk-imported holiday must be retrievable via GET?year=");
        returnedNames.Should().Contain($"BulkRead Holiday B {suffix}",
            "the second bulk-imported holiday must be retrievable via GET?year=");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-013: POST /api/public-holidays/bulk with duplicate entries —
    //                 second import skips duplicates, returns only newly created
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-013: When POST /api/public-holidays/bulk is called twice with the same
    /// Name+Date pairs, the second call must skip the existing records and return an empty
    /// or partial list — no duplicate holiday records must be created.
    /// This verifies the idempotency guarantee of BulkImportAsync (AC-63 duplicate-skip).
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_013_POST_BulkImport_DuplicateEntries_SkippedOnSecondImport()
    {
        // Arrange — unique suffix ensures these holidays don't clash with IT-F10-INT-011 or -012
        var client = CreateAuthenticatedClient(RoleNames.SuperAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var payload = new[]
        {
            new { name = $"Duplicate Holiday {suffix}", date = "2029-05-01", countryCode = "IN", isOptional = false }
        };

        // Act — first import (should create 1 holiday)
        var firstResponse = await client.PostAsJsonAsync("/api/public-holidays/bulk", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, "first import must succeed with 200 OK");
        var firstEnvelope = await firstResponse.Content.ReadFromJsonAsync<BulkImportResponseShape>(JsonOptions);
        firstEnvelope!.Data.Should().NotBeNull("first import response.body.data must be present");
        firstEnvelope.Data!.Should().HaveCount(1, "first import of 1 unique holiday must return 1 created record");

        // Act — second import with the same payload (all duplicates)
        var secondResponse = await client.PostAsJsonAsync("/api/public-holidays/bulk", payload);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "second import must still return 200 OK even if all entries are duplicates");

        var secondEnvelope = await secondResponse.Content.ReadFromJsonAsync<BulkImportResponseShape>(JsonOptions);
        secondEnvelope!.Data.Should().NotBeNull("second import response.body.data must be present");

        // Assert — no new records were created (duplicates skipped)
        secondEnvelope.Data!.Should().HaveCount(0,
            "all entries in the second import are duplicates — BulkImportAsync must skip them and return empty list");

        // Assert — GET still shows exactly 1 record for this suffix (no duplicates persisted)
        var getResponse = await client.GetAsync("/api/public-holidays?year=2029");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiListResponseShape>(JsonOptions);
        getEnvelope!.Data.Should().NotBeNull();

        var matchingHolidays = getEnvelope.Data!
            .Where(h => h.Name == $"Duplicate Holiday {suffix}")
            .ToList();
        matchingHolidays.Should().HaveCount(1,
            "exactly 1 record must exist after two imports of the same holiday — no duplicates must be persisted");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper shapes for deserializing ApiResponse envelope
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class BulkImportResponseShape
    {
        public List<HolidayItemShape>? Data { get; set; }
    }

    private sealed class ApiListResponseShape
    {
        public List<HolidayItemShape>? Data { get; set; }
    }

    private sealed class HolidayItemShape
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int Year { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public bool IsActive { get; set; }
    }
}
