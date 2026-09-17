// Integration tests for F-03: Department Management.
// See IT-F03-001 through IT-F03-007 in docs/test-plan.md.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// department-management behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-03 — Department Management.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered: IT-F03-001, IT-F03-002, IT-F03-003, IT-F03-004, IT-F03-005, IT-F03-006, IT-F03-007.
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class DepartmentIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DepartmentIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

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
    /// POSTs a new department with unique name/code and returns the raw <see cref="HttpResponseMessage"/>.
    /// Caller is responsible for disposing the response.
    /// </summary>
    private Task<HttpResponseMessage> CreateDepartmentAsync(HttpClient client, string name, string code, int overlapLimit = 0)
    {
        var payload = new { name, code, overlapLimit };
        return client.PostAsJsonAsync("/api/departments", payload);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-001: GET /api/departments returns 200 with { data: [...] } envelope
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-001: An authenticated GET request to /api/departments must return 200 OK
    /// and the response body must contain a "data" property (ApiResponse&lt;T&gt; envelope).
    /// The in-memory database may be empty; the property must still be present and non-null.
    /// </summary>
    [Fact]
    public async Task IT_F03_001_GET_Departments_Returns200_WithDataEnvelope()
    {
        // Arrange — any authenticated role may read the department list.
        var client = CreateAuthenticatedClient(RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/departments");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/departments must return 200 OK for authenticated users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-002: GET /api/departments/{id} for existing dept returns { data: DepartmentDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-002: A GET request for an existing department ID must return 200 OK with
    /// a "data" object whose properties match the department that was just created.
    /// </summary>
    [Fact]
    public async Task IT_F03_002_GET_DepartmentById_ExistingId_Returns200_WithDepartmentDto()
    {
        // Arrange — create a department as HRAdmin first so there is something to fetch.
        var adminClient = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateDepartmentAsync(adminClient, $"Engineering-{uniqueSuffix}", $"ENG{uniqueSuffix[..3]}");
        createResponse.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "pre-condition: creating a department with HRAdmin role must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<DepartmentResponseShape>(JsonOptions);
        createEnvelope!.Data.Should().NotBeNull("pre-condition: created department must have a data body");
        var createdId = createEnvelope.Data!.Id;

        // Act — read by the newly created ID using an Employee (read-only) client.
        var readerClient = CreateAuthenticatedClient(RoleNames.Employee);
        var response = await readerClient.GetAsync($"/api/departments/{createdId}");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/departments/{id} for an existing department must return 200 OK");

        // Assert — envelope and DTO shape
        var envelope = await response.Content.ReadFromJsonAsync<DepartmentResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain a DepartmentDto");
        envelope.Data!.Id.Should().Be(createdId, "the returned department ID must match the requested ID");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-003: GET /api/departments/{nonexistent} returns 404
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-003: A GET request for a department ID that does not exist must return 404.
    /// </summary>
    [Fact]
    public async Task IT_F03_003_GET_DepartmentById_NonexistentId_Returns404()
    {
        // Arrange — use a random GUID that was never inserted.
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/departments/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "GET /api/departments/{id} for a nonexistent ID must return 404");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-004: POST /api/departments with HRAdmin role creates dept → { data: DepartmentDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-004: A POST request from an HRAdmin must create the department and return
    /// 201 Created with a "data" object containing the new DepartmentDto.
    /// The response must include the assigned GUID and the submitted name/code.
    /// </summary>
    [Fact]
    public async Task IT_F03_004_POST_Departments_HRAdminRole_Creates_Returns201_WithDepartmentDto()
    {
        // Arrange — unique name/code to avoid code-conflict with other test runs.
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var name = $"HR-Dept-{uniqueSuffix}";
        var code = $"HR{uniqueSuffix[..4]}";

        // Act
        var response = await CreateDepartmentAsync(client, name, code, overlapLimit: 5);

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "POST /api/departments with HRAdmin role must return 201 Created");

        // Assert — response body has data property (ApiResponse<T> envelope)
        var envelope = await response.Content.ReadFromJsonAsync<DepartmentResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the created DepartmentDto");

        // Assert — DTO fields match submitted values
        envelope.Data!.Id.Should().NotBeEmpty("the created department must have an assigned ID");
        envelope.Data.Name.Should().Be(name, "returned name must match submitted name");
        envelope.Data.Code.Should().Be(code, "returned code must match submitted code");
        envelope.Data.OverlapLimit.Should().Be(5, "returned overlapLimit must match submitted value");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-005: POST /api/departments with Employee role returns 403
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-005: A POST request authenticated as "Employee" must be rejected with 403 Forbidden.
    /// The [Authorize(Roles = "HRAdmin,Director")] attribute on POST must deny this request
    /// before business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F03_005_POST_Departments_EmployeeRole_Returns403()
    {
        // Arrange — Employee role only; not in HRAdmin or Director.
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        // Act
        var response = await CreateDepartmentAsync(client, $"Blocked-{uniqueSuffix}", $"BLK{uniqueSuffix[..3]}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be authorized to POST to /api/departments; " +
            "RBAC must return 403 after authentication succeeds");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-006: PUT /api/departments/{id} updates dept → { data: DepartmentDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-006: A PUT request from an HRAdmin with valid fields must update the department
    /// and return 200 OK with a "data" object containing the updated DepartmentDto.
    /// </summary>
    [Fact]
    public async Task IT_F03_006_PUT_Department_HRAdminRole_Updates_Returns200_WithDepartmentDto()
    {
        // Arrange — create a department to update.
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateDepartmentAsync(client, $"OriginalName-{uniqueSuffix}", $"ORI{uniqueSuffix[..3]}");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: department creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<DepartmentResponseShape>(JsonOptions);
        var departmentId = createEnvelope!.Data!.Id;

        // Prepare update payload — new name, same code, new overlapLimit.
        var updatedName = $"UpdatedName-{uniqueSuffix}";
        var updatePayload = new { name = updatedName, overlapLimit = 10 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/departments/{departmentId}", updatePayload);

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "PUT /api/departments/{id} with HRAdmin role must return 200 OK");

        // Assert — envelope and updated DTO
        var envelope = await response.Content.ReadFromJsonAsync<DepartmentResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the updated DepartmentDto");
        envelope.Data!.Id.Should().Be(departmentId, "the updated department ID must not change");
        envelope.Data.Name.Should().Be(updatedName, "the updated name must be reflected in the response");
        envelope.Data.OverlapLimit.Should().Be(10, "the updated overlapLimit must be reflected in the response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F03-007: DELETE /api/departments/{id} soft-deletes → 200 with success
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F03-007: A DELETE request from an HRAdmin must soft-delete the department and
    /// return 200 OK. After deletion, a subsequent GET by the same ID must return 404,
    /// confirming the record is no longer visible (soft-deleted).
    /// </summary>
    [Fact]
    public async Task IT_F03_007_DELETE_Department_HRAdminRole_SoftDeletes_Returns200()
    {
        // Arrange — create a department to delete.
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateDepartmentAsync(client, $"ToDelete-{uniqueSuffix}", $"DEL{uniqueSuffix[..3]}");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: department creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<DepartmentResponseShape>(JsonOptions);
        var departmentId = createEnvelope!.Data!.Id;

        // Act — soft-delete the department.
        var deleteResponse = await client.DeleteAsync($"/api/departments/{departmentId}");

        // Assert — delete returns 200 OK (soft-delete; the record is marked deleted, not removed).
        deleteResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "DELETE /api/departments/{id} with HRAdmin role must return 200 OK after a soft-delete");

        // Assert — response has data envelope
        var deleteEnvelope = await deleteResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        deleteEnvelope.Should().NotBeNull("the delete response must be wrapped in ApiResponse<T>");
        deleteEnvelope!.Data.Should().NotBeNull("response.body.data must be present after a successful soft-delete");

        // Assert — subsequent GET returns 404 (soft-deleted record is no longer visible).
        var getResponse = await client.GetAsync($"/api/departments/{departmentId}");
        getResponse.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "a GET request for a soft-deleted department must return 404 — the record must not be visible after deletion");
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
    /// Shape for deserializing a department-specific ApiResponse envelope.
    /// Only fields referenced by assertions are declared.
    /// </summary>
    private sealed class DepartmentResponseShape
    {
        public DepartmentDataShape? Data { get; set; }
    }

    private sealed class DepartmentDataShape
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int OverlapLimit { get; set; }
        public bool IsActive { get; set; }
    }
}
