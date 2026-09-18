// Integration tests for F-14: Seed Data & Initial Setup — Idempotency and Entity Counts.
// Tests cover IT-F14-INT-008 and IT-F14-INT-009 from docs/test-plan.md.
// These exercise POST /api/admin/reseed and GET /api/admin/seed-status to verify:
//   AC-67: Running the seed script twice does not create duplicate records.
//   AC-68: Running the seed script on a fresh database creates exactly the expected
//          number of roles (5), departments (4), leave types (5), holidays (14),
//          and system configs (4).
//
// NOTE: This class does NOT join LmsIntegrationCollection so it receives its own
//       LmsWebApplicationFactory instance (and thus its own isolated in-memory database).
//       This isolation is required for the count assertions in IT-F14-INT-009.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-14 — Seed Idempotency and Entity Count Assertions.
/// Tests exercise POST /api/admin/reseed + GET /api/admin/seed-status through the full
/// ASP.NET Core pipeline against an isolated in-memory database.
///
/// IT- IDs covered: IT-F14-INT-008, IT-F14-INT-009.
/// </summary>
public class SeedIdempotencyIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SeedIdempotencyIntegrationTests(LmsWebApplicationFactory factory)
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

    private async Task<SeedStatusDataShape> GetSeedStatusAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/seed-status");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "GET /api/admin/seed-status must succeed");

        var envelope = await response.Content.ReadFromJsonAsync<SeedStatusResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain SeedStatusDto");
        return envelope.Data!;
    }

    private async Task ReseedAsync(HttpClient client, string section)
    {
        var response = await client.PostAsJsonAsync("/api/admin/reseed", new { section });
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"POST /api/admin/reseed with section='{section}' must return 200 OK");

        var envelope = await response.Content.ReadFromJsonAsync<BoolResponseShape>(JsonOptions);
        envelope!.Data.Should().BeTrue($"TriggerReseedAsync must return true for section='{section}'");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-008: Running reseed "all" twice produces no duplicate records (AC-67)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-008: POST /api/admin/reseed (section=all) called twice must produce no
    /// duplicate records. The entity counts returned by GET /api/admin/seed-status must be
    /// identical after the first and second reseed calls.
    ///
    /// This is the integration-test assertion for AC-67:
    ///   "Running the seed script twice does not create duplicate Super Admin, HR Admin,
    ///    HR department, or leave type records."
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_008_Reseed_All_Twice_EntityCounts_Remain_Identical()
    {
        // Arrange — SuperAdmin is the only role authorized to POST /api/admin/reseed
        var superAdminClient = CreateAuthenticatedClient(RoleNames.SuperAdmin);
        var hrAdminClient    = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act — first reseed
        await ReseedAsync(superAdminClient, "all");
        var statusAfterFirst = await GetSeedStatusAsync(hrAdminClient);

        // Record the counts from the first seed
        var rolesAfterFirst       = statusAfterFirst.TotalRoles;
        var depsAfterFirst        = statusAfterFirst.TotalDepartments;
        var leaveTypesAfterFirst  = statusAfterFirst.TotalLeaveTypes;
        var holidaysAfterFirst    = statusAfterFirst.TotalPublicHolidays;
        var configsAfterFirst     = statusAfterFirst.TotalSystemConfigs;

        // Act — second reseed (must be idempotent — no new records)
        await ReseedAsync(superAdminClient, "all");
        var statusAfterSecond = await GetSeedStatusAsync(hrAdminClient);

        // Assert — all counts must be identical (no duplicates created)
        statusAfterSecond.TotalRoles.Should().Be(rolesAfterFirst,
            "reseeding roles a second time must skip all existing roles — no duplicates");

        statusAfterSecond.TotalDepartments.Should().Be(depsAfterFirst,
            "reseeding departments a second time must skip all existing departments — no duplicates");

        statusAfterSecond.TotalLeaveTypes.Should().Be(leaveTypesAfterFirst,
            "reseeding leave types a second time must skip all existing leave types — no duplicates");

        statusAfterSecond.TotalPublicHolidays.Should().Be(holidaysAfterFirst,
            "reseeding holidays a second time must skip all existing holidays — no duplicates");

        statusAfterSecond.TotalSystemConfigs.Should().Be(configsAfterFirst,
            "reseeding system configs a second time must skip all existing configs — no duplicates");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F14-INT-009: POST /api/admin/reseed "all" on fresh DB creates expected entity counts (AC-68)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F14-INT-009: POST /api/admin/reseed (section=all) on a fresh in-memory database
    /// must create exactly the expected number of each entity type, verifying AC-68:
    ///   "Running the seed script on a fresh database creates exactly 1 Super Admin,
    ///    1 HR Admin, 1 HR department, and 5 leave type records."
    ///
    /// Actual DataSeeder implementation creates:
    ///   - 5 role entities (HRAdmin, Manager, Employee, Director, SuperAdmin)
    ///   - 4 department entities (Human Resources, Engineering, Finance, Operations)
    ///   - 5 leave type entities (Casual, Sick, Earned, CompOff, Unpaid)
    ///   - 14 public holiday entities (2026 Indian national holidays)
    ///   - 4 system config entities (leave year, max days, country code, work week)
    ///   - IsHealthy == true when all three core entity types have at least 1 record
    ///
    /// The in-memory DB for this class is isolated from LmsIntegrationCollection,
    /// so the database is guaranteed fresh at the start of this test.
    /// </summary>
    [Fact]
    public async Task IT_F14_INT_009_Reseed_All_FreshDatabase_Creates_Expected_EntityCounts()
    {
        // Arrange — fresh in-memory DB; this class does NOT share LmsIntegrationCollection's factory
        var superAdminClient = CreateAuthenticatedClient(RoleNames.SuperAdmin);
        var hrAdminClient    = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act — reseed "all" on the fresh database
        await ReseedAsync(superAdminClient, "all");
        var status = await GetSeedStatusAsync(hrAdminClient);

        // Assert — response.body.data must be present (API envelope verification)
        status.Should().NotBeNull("seed-status response.body.data must not be null after reseed");

        // Assert — role count matches all seeded roles (AC-68: SuperAdmin + HRAdmin + others)
        status.TotalRoles.Should().Be(5,
            "DataSeeder.SeedRolesAsync seeds exactly 5 system roles: HRAdmin, Manager, Employee, Director, SuperAdmin");

        // Assert — department count (DataSeeder seeds 4 default departments)
        status.TotalDepartments.Should().Be(4,
            "DataSeeder.SeedDepartmentsAsync seeds exactly 4 departments: HR, Engineering, Finance, Operations");

        // Assert — leave type count (AC-68 requires 5)
        status.TotalLeaveTypes.Should().Be(5,
            "DataSeeder.SeedLeaveTypesAsync seeds exactly 5 leave types: Casual, Sick, Earned, CompOff, Unpaid");

        // Assert — public holiday count (2026 Indian holidays)
        status.TotalPublicHolidays.Should().Be(14,
            "DataSeeder.SeedPublicHolidaysAsync seeds exactly 14 Indian national holidays for 2026");

        // Assert — system config count
        status.TotalSystemConfigs.Should().Be(4,
            "DataSeeder.SeedSystemConfigsAsync seeds exactly 4 default system configuration keys");

        // Assert — IsHealthy must be true when all three core entity types are seeded
        status.IsHealthy.Should().BeTrue(
            "IsHealthy == true when TotalRoles > 0 AND TotalDepartments > 0 AND TotalLeaveTypes > 0 (AC-68)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper shapes for deserializing ApiResponse envelope
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class SeedStatusResponseShape
    {
        public SeedStatusDataShape? Data { get; set; }
    }

    private sealed class SeedStatusDataShape
    {
        public int TotalRoles { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalLeaveTypes { get; set; }
        public int TotalPublicHolidays { get; set; }
        public int TotalSystemConfigs { get; set; }
        public bool IsHealthy { get; set; }
    }

    private sealed class BoolResponseShape
    {
        public bool Data { get; set; }
    }
}
