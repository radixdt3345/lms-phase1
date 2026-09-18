// Integration tests for F-10: Public Holiday Management.
// Tests cover IT-F10-INT-001 through IT-F10-INT-008 from docs/test-plan.md.
// These exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// Key mismatch fix (INT layer): frontend field names aligned to backend (countryCode/isOptional)
// and API response verified to wrap in { "data": T } envelope.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-10 — Public Holiday Management.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, service, in-memory EF Core) is real.
///
/// IT- IDs covered: IT-F10-INT-001 through IT-F10-INT-008.
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class PublicHolidayIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PublicHolidayIntegrationTests(LmsWebApplicationFactory factory)
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

    /// <summary>
    /// POSTs a new public holiday and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    private Task<HttpResponseMessage> CreateHolidayAsync(
        HttpClient client,
        string name,
        string date,
        string countryCode = "IN",
        bool isOptional = false,
        string? description = null)
    {
        var payload = new
        {
            name,
            date,
            countryCode,
            isOptional,
            description
        };
        return client.PostAsJsonAsync("/api/public-holidays", payload);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-001: GET /api/public-holidays?year={year} returns 200 with { data: [...] } envelope
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-001: An authenticated GET for a given year must return 200 OK and the
    /// body must contain a "data" array (ApiResponse&lt;T&gt; envelope).
    /// This verifies the no-key-mismatch guarantee: response.data.data must be reachable.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_001_GET_PublicHolidays_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var year = DateTime.UtcNow.Year;

        // Act
        var response = await client.GetAsync($"/api/public-holidays?year={year}");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/public-holidays?year={year} must return 200 for authenticated users");

        // Assert — ApiResponse<T> envelope has "data" property (frontend reads response.data.data)
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-002: POST /api/public-holidays returns 401 without auth header
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-002: An unauthenticated POST must be rejected with 401 Unauthorized.
    /// The [Authorize] attribute on the controller must deny this request.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_002_POST_PublicHoliday_NoAuth_Returns401()
    {
        // Arrange — no auth header; TestAuthHandler returns Fail when X-Test-Auth-Fail is present
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");

        // Act
        var payload = new { name = "Test Holiday", date = "2026-08-15", countryCode = "IN", isOptional = false };
        var response = await client.PostAsJsonAsync("/api/public-holidays", payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "an unauthenticated request to POST /api/public-holidays must return 401");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-003: POST /api/public-holidays with Employee role returns 403
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-003: A POST request authenticated as "Employee" must be rejected with 403.
    /// The [Authorize(Roles = "HRAdmin,SuperAdmin")] attribute must deny this request.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_003_POST_PublicHoliday_EmployeeRole_Returns403()
    {
        // Arrange — Employee role is not in HRAdmin or SuperAdmin
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await CreateHolidayAsync(client, "Independence Day", "2026-08-15");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee must not be authorized to create public holidays; RBAC must return 403");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-004: POST /api/public-holidays with HRAdmin creates → { data: PublicHolidayDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-004: A POST request from HRAdmin must create the holiday and return 201 Created
    /// with a "data" object. The response shape must include the backend field names
    /// (countryCode, isOptional, isActive) — not the old frontend names (country, isRecurring).
    /// This test is the key mismatch verification for the INT layer.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_004_POST_PublicHoliday_HRAdminRole_Creates_Returns201_WithCorrectFields()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var name = $"Integration Day {suffix}";
        var date = "2026-06-15";
        var countryCode = "IN";

        // Act
        var response = await CreateHolidayAsync(client, name, date, countryCode, isOptional: false,
            description: "INT layer test holiday");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "POST /api/public-holidays with HRAdmin role must return 201 Created");

        // Assert — envelope shape (verifies response.data.data is reachable from frontend)
        var envelope = await response.Content.ReadFromJsonAsync<HolidayResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain a PublicHolidayDto");

        // Assert — backend field names present (key mismatch check: not 'country', not 'isRecurring')
        envelope.Data!.Id.Should().NotBeEmpty("the created holiday must have an assigned ID");
        envelope.Data.Name.Should().Be(name, "name must match submitted value");
        envelope.Data.CountryCode.Should().Be(countryCode,
            "API must return 'countryCode' not 'country' — frontend reads response.data.data.countryCode");
        envelope.Data.IsOptional.Should().BeFalse(
            "API must return 'isOptional' not 'isRecurring' — frontend reads response.data.data.isOptional");
        envelope.Data.IsActive.Should().BeTrue("newly created holidays must default to active");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-005: GET /api/public-holidays/{id} for existing holiday → { data: T }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-005: GET by ID for an existing holiday must return 200 with correct data.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_005_GET_PublicHolidayById_ExistingId_Returns200_WithDto()
    {
        // Arrange — create a holiday first
        var adminClient = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateHolidayAsync(adminClient, $"Test Holiday {suffix}", "2026-04-14");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<HolidayResponseShape>(JsonOptions);
        var createdId = createEnvelope!.Data!.Id;

        // Act — read by ID as Employee
        var readerClient = CreateAuthenticatedClient(RoleNames.Employee);
        var response = await readerClient.GetAsync($"/api/public-holidays/{createdId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/public-holidays/{id} for an existing holiday must return 200 OK");

        var envelope = await response.Content.ReadFromJsonAsync<HolidayResponseShape>(JsonOptions);
        envelope!.Data!.Id.Should().Be(createdId, "returned ID must match requested ID");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-006: GET /api/public-holidays/{nonexistent} returns 404
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-006: GET for a non-existent holiday ID must return 404.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_006_GET_PublicHolidayById_NonexistentId_Returns404()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/public-holidays/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "GET /api/public-holidays/{id} for a nonexistent ID must return 404");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-007: PUT /api/public-holidays/{id} updates holiday → { data: T }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-007: PUT from HRAdmin must update the holiday and return 200 OK with updated DTO.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_007_PUT_PublicHoliday_HRAdminRole_Updates_Returns200()
    {
        // Arrange — create holiday to update
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateHolidayAsync(client, $"OriginalHoliday {suffix}", "2026-05-01");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<HolidayResponseShape>(JsonOptions);
        var holidayId = createEnvelope!.Data!.Id;

        // Act — update with new name
        var updatedName = $"UpdatedHoliday {suffix}";
        var updatePayload = new { name = updatedName, isOptional = true };
        var response = await client.PutAsJsonAsync($"/api/public-holidays/{holidayId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "PUT /api/public-holidays/{id} with HRAdmin role must return 200 OK");

        var envelope = await response.Content.ReadFromJsonAsync<HolidayResponseShape>(JsonOptions);
        envelope!.Data!.Name.Should().Be(updatedName, "updated name must be reflected in response");
        envelope.Data.IsOptional.Should().BeTrue("updated isOptional must be reflected in response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F10-INT-008: DELETE /api/public-holidays/{id} removes holiday → subsequent GET 404
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F10-INT-008: DELETE from HRAdmin must remove the holiday.
    /// Subsequent GET by same ID must return 404.
    /// </summary>
    [Fact]
    public async Task IT_F10_INT_008_DELETE_PublicHoliday_HRAdminRole_Removes_Returns200()
    {
        // Arrange — create holiday to delete
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateHolidayAsync(client, $"ToDelete {suffix}", "2026-12-25");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<HolidayResponseShape>(JsonOptions);
        var holidayId = createEnvelope!.Data!.Id;

        // Act — delete
        var deleteResponse = await client.DeleteAsync($"/api/public-holidays/{holidayId}");

        // Assert — delete returns 200 OK
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "DELETE /api/public-holidays/{id} with HRAdmin must return 200 OK");

        // Assert — subsequent GET returns 404
        var getResponse = await client.GetAsync($"/api/public-holidays/{holidayId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "GET for a deleted holiday must return 404 — record must not be visible after deletion");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper shapes for deserializing ApiResponse envelope
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }

    private sealed class HolidayResponseShape
    {
        public HolidayDataShape? Data { get; set; }
    }

    private sealed class HolidayDataShape
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? Description { get; set; }
        // Backend field name: countryCode (not 'country') — INT layer key mismatch fix
        public string CountryCode { get; set; } = string.Empty;
        // Backend field name: isOptional (not 'isRecurring') — INT layer key mismatch fix
        public bool IsOptional { get; set; }
        public bool IsActive { get; set; }
    }
}
