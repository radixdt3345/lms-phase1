// Integration tests for F-02: Employee Management.
// IT- IDs covered: IT-F02-001 through IT-F02-011.
// Tests exercise the full ASP.NET Core pipeline: middleware → controller → service → in-memory database.
// If any of these tests start failing after a change, that change altered observable
// employee-management behavior — confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-02 — Employee Management.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters, service, in-memory EF Core) is real.
///
/// IT- IDs covered:
///   IT-F02-001: GET /api/employees returns 200 with { data: [...] } for HRAdmin
///   IT-F02-002: GET /api/employees returns 401 when unauthenticated
///   IT-F02-003: POST /api/employees creates employee and returns 201 with { data: EmployeeProfileDto }
///   IT-F02-004: GET /api/employees/{id} returns 200 with { data: EmployeeProfileDto }
///   IT-F02-005: PUT /api/employees/{id} updates employee and returns 200 with { data: EmployeeProfileDto }
///   IT-F02-006: DELETE /api/employees/{id} soft-deletes and returns 200
///   IT-F02-007: GET /api/employees with Manager role returns 200
///   IT-F02-008: POST /api/employees with invalid data returns 400
///   IT-F02-009: POST /api/employees with Manager role returns 403 (AC-18)
///   IT-F02-010: POST /api/employees without ManagerUserId creates employee (AC-17)
///   IT-F02-011: GET /api/employees/{nonexistentId} returns 404
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class EmployeeIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeIntegrationTests(LmsWebApplicationFactory factory)
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
    /// Creates an unauthenticated HttpClient (no role header, no auth headers).
    /// The TestAuthHandler will still process this but with no fail header —
    /// use X-Test-Auth-Fail to drive a real 401.
    /// </summary>
    private HttpClient CreateUnauthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.FailHeader, "true");
        return client;
    }

    /// <summary>
    /// Builds a valid CreateEmployeeProfileDto payload with a unique employee code.
    /// </summary>
    private static object BuildCreatePayload(
        string? employeeCode = null,
        Guid? userId = null,
        Guid? managerUserId = null,
        string? joiningDate = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return new
        {
            userId = userId ?? Guid.NewGuid(),
            employeeCode = employeeCode ?? $"EMP-{suffix}",
            jobTitle = "Software Engineer",
            joiningDate = joiningDate ?? "2024-01-15",
            employmentType = "Full-Time",
            managerUserId,
        };
    }

    /// <summary>
    /// POSTs a new employee and returns the raw <see cref="HttpResponseMessage"/>.
    /// Caller is responsible for disposing the response.
    /// </summary>
    private Task<HttpResponseMessage> CreateEmployeeAsync(HttpClient client, object? payload = null)
    {
        payload ??= BuildCreatePayload();
        return client.PostAsJsonAsync("/api/employees", payload);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-001: GET /api/employees returns 200 with { data: [...] } for HRAdmin
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-001: An authenticated GET request to /api/employees must return 200 OK
    /// and the response body must contain a "data" property (ApiResponse&lt;T&gt; envelope).
    /// The in-memory database may be empty; the property must still be present and non-null.
    /// Covers AC-16 (authenticated access returns employee list).
    /// </summary>
    [Fact]
    public async Task IT_F02_001_GET_Employees_HRAdminRole_Returns200_WithDataEnvelope()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/employees must return 200 OK for authenticated HRAdmin users");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for a 200 response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-002: GET /api/employees returns 401 when unauthenticated
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-002: A GET request with no valid token must return 401 Unauthorized.
    /// The [Authorize] attribute at the controller class level must block the request
    /// before any business logic executes.
    /// </summary>
    [Fact]
    public async Task IT_F02_002_GET_Employees_Unauthenticated_Returns401()
    {
        // Arrange — client drives TestAuthHandler to simulate auth failure
        var client = CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "GET /api/employees without a valid token must return 401 Unauthorized");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-003: POST /api/employees creates employee and returns 201 with { data: employee }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-003: A POST request from an HRAdmin with a complete, valid body must
    /// create the employee profile and return 201 Created with a "data" object
    /// containing the new EmployeeProfileDto. Covers AC-16.
    /// </summary>
    [Fact]
    public async Task IT_F02_003_POST_Employees_HRAdminRole_Creates_Returns201_WithEmployeeDto()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var payload = BuildCreatePayload(employeeCode: $"EMP-{suffix}", joiningDate: "2024-03-01");

        // Act
        var response = await CreateEmployeeAsync(client, payload);

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "POST /api/employees with HRAdmin role and valid body must return 201 Created");

        // Assert — ApiResponse<T> envelope has "data" property
        var envelope = await response.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the created EmployeeProfileDto");

        // Assert — DTO fields
        envelope.Data!.Id.Should().NotBeEmpty("the created employee must have an assigned ID");
        envelope.Data.EmployeeCode.Should().NotBeNullOrEmpty("the returned employee must include the employee code");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-004: GET /api/employees/{id} returns 200 with { data: employee }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-004: A GET request for an existing employee ID must return 200 OK
    /// with a "data" object whose fields match the employee that was just created.
    /// </summary>
    [Fact]
    public async Task IT_F02_004_GET_EmployeeById_ExistingId_Returns200_WithEmployeeDto()
    {
        // Arrange — create an employee first so there is something to fetch
        var adminClient = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var payload = BuildCreatePayload(employeeCode: $"EMP-{suffix}");
        var createResponse = await CreateEmployeeAsync(adminClient, payload);
        createResponse.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "pre-condition: creating an employee with HRAdmin role must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        createEnvelope!.Data.Should().NotBeNull("pre-condition: created employee must have a data body");
        var createdId = createEnvelope.Data!.Id;

        // Act — read by the newly created ID
        var readerClient = CreateAuthenticatedClient(RoleNames.Employee);
        var response = await readerClient.GetAsync($"/api/employees/{createdId}");

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/employees/{id} for an existing employee must return 200 OK");

        // Assert — envelope and DTO shape
        var envelope = await response.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain an EmployeeProfileDto");
        envelope.Data!.Id.Should().Be(createdId, "the returned employee ID must match the requested ID");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-005: PUT /api/employees/{id} updates employee and returns 200
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-005: A PUT request from an HRAdmin with valid fields must update the employee
    /// profile and return 200 OK with a "data" object containing the updated EmployeeProfileDto.
    /// </summary>
    [Fact]
    public async Task IT_F02_005_PUT_Employee_HRAdminRole_Updates_Returns200_WithEmployeeDto()
    {
        // Arrange — create an employee to update
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateEmployeeAsync(client, BuildCreatePayload(employeeCode: $"EMP-{suffix}"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: employee creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        var employeeId = createEnvelope!.Data!.Id;

        // Prepare update payload — new job title
        var updatePayload = new { jobTitle = "Senior Software Engineer", employmentType = "Full-Time" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/employees/{employeeId}", updatePayload);

        // Assert — status
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "PUT /api/employees/{id} with HRAdmin role must return 200 OK");

        // Assert — envelope and updated DTO
        var envelope = await response.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the updated EmployeeProfileDto");
        envelope.Data!.Id.Should().Be(employeeId, "the updated employee ID must not change");
        envelope.Data.JobTitle.Should().Be("Senior Software Engineer",
            "the updated job title must be reflected in the response");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-006: DELETE /api/employees/{id} soft-deletes and returns 200
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-006: A DELETE request from an HRAdmin must soft-delete the employee profile
    /// and return 200 OK with a "data" envelope. Covers AC-19.
    ///
    /// Note: the controller returns 200 OK (not 204) with ApiResponse&lt;bool&gt;.
    /// </summary>
    [Fact]
    public async Task IT_F02_006_DELETE_Employee_HRAdminRole_SoftDeletes_Returns200()
    {
        // Arrange — create an employee to delete
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await CreateEmployeeAsync(client, BuildCreatePayload(employeeCode: $"EMP-DEL-{suffix}"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "pre-condition: employee creation must succeed");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        var employeeId = createEnvelope!.Data!.Id;

        // Act — soft-delete the employee
        var deleteResponse = await client.DeleteAsync($"/api/employees/{employeeId}");

        // Assert — delete returns 200 OK (soft-delete; the record is marked deleted, not removed)
        deleteResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "DELETE /api/employees/{id} with HRAdmin role must return 200 OK after a soft-delete (AC-19)");

        // Assert — response has data envelope
        var deleteEnvelope = await deleteResponse.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        deleteEnvelope.Should().NotBeNull("the delete response must be wrapped in ApiResponse<T>");
        deleteEnvelope!.Data.Should().NotBeNull("response.body.data must be present after a successful soft-delete");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-007: GET /api/employees with Manager role returns 200
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-007: A GET request authenticated as Manager must return 200 OK.
    /// The read endpoint has only [Authorize] (no role restriction), so all
    /// authenticated roles, including Manager, must have read access.
    /// </summary>
    [Fact]
    public async Task IT_F02_007_GET_Employees_ManagerRole_Returns200()
    {
        // Arrange
        var client = CreateAuthenticatedClient(RoleNames.Manager);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/employees with Manager role must return 200 OK — read access is not restricted to HRAdmin");

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must be present for an authenticated read");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-008: POST /api/employees with invalid data returns 400
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-008: A POST request from an HRAdmin with a body that fails model validation
    /// (missing required fields) must return 400 Bad Request before reaching business logic.
    /// </summary>
    [Fact]
    public async Task IT_F02_008_POST_Employees_InvalidData_Returns400()
    {
        // Arrange — omit all required fields to trigger model validation failure
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var invalidPayload = new { jobTitle = "Ghost Engineer" }; // missing UserId, EmployeeCode, JoiningDate

        // Act
        var response = await CreateEmployeeAsync(client, invalidPayload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "POST /api/employees with missing required fields must return 400 Bad Request");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-009: POST /api/employees with Manager role returns 403 (AC-18)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-009: A POST request authenticated as Manager must be rejected with 403 Forbidden.
    /// The [Authorize(Roles = "HRAdmin,SuperAdmin")] attribute on POST must deny this request.
    /// Covers AC-18.
    /// </summary>
    [Fact]
    public async Task IT_F02_009_POST_Employees_ManagerRole_Returns403()
    {
        // Arrange — Manager role is not in HRAdmin or SuperAdmin
        var client = CreateAuthenticatedClient(RoleNames.Manager);
        var payload = BuildCreatePayload();

        // Act
        var response = await CreateEmployeeAsync(client, payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "a Manager role must not be authorized to POST to /api/employees — RBAC must return 403 (AC-18)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-010: POST /api/employees without ManagerUserId creates employee (AC-17)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-010: A POST request from HRAdmin without a reporting manager ID must still
    /// create the employee successfully and return 201 Created. Covers AC-17.
    /// </summary>
    [Fact]
    public async Task IT_F02_010_POST_Employees_WithoutManagerUserId_CreatesEmployee_Returns201()
    {
        // Arrange — no managerUserId supplied
        var client = CreateAuthenticatedClient(RoleNames.HRAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var payload = new
        {
            userId = Guid.NewGuid(),
            employeeCode = $"EMP-NM-{suffix}",
            jobTitle = "Individual Contributor",
            joiningDate = "2024-06-01",
            employmentType = "Full-Time",
            // managerUserId intentionally omitted (AC-17)
        };

        // Act
        var response = await CreateEmployeeAsync(client, payload);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "POST /api/employees without reporting_manager_id must still create the employee (AC-17)");

        var envelope = await response.Content.ReadFromJsonAsync<EmployeeResponseShape>(JsonOptions);
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull("response.body.data must contain the created EmployeeProfileDto");
        envelope.Data!.Id.Should().NotBeEmpty("the created employee must have an assigned ID");
        envelope.Data.ManagerUserId.Should().BeNull(
            "ManagerUserId must be null when no reporting manager was supplied (AC-17)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F02-011: GET /api/employees/{nonexistentId} returns 404
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F02-011: A GET request for an employee ID that does not exist must return 404.
    /// </summary>
    [Fact]
    public async Task IT_F02_011_GET_EmployeeById_NonexistentId_Returns404()
    {
        // Arrange — use a random GUID that was never inserted
        var client = CreateAuthenticatedClient(RoleNames.Employee);
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/employees/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "GET /api/employees/{id} for a nonexistent ID must return 404");
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
    /// Shape for deserializing an employee-specific ApiResponse envelope.
    /// Only fields referenced by assertions are declared.
    /// </summary>
    private sealed class EmployeeResponseShape
    {
        public EmployeeDataShape? Data { get; set; }
    }

    private sealed class EmployeeDataShape
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string EmploymentType { get; set; } = "Full-Time";
        public Guid? ManagerUserId { get; set; }
        public string? ManagerName { get; set; }
        public Guid? DepartmentId { get; set; }
    }
}
