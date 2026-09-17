// Integration tests for F-04: Leave Type & Policy Management.
// Covers IT-F04-001 through IT-F04-007 from docs/test-plan.md.
// These tests exercise the full ASP.NET Core pipeline: middleware → controller → service → response.
// JWT validation is provided by TestAuthHandler; the database is an in-memory EF Core store.
// If a test starts failing after a code change, that change altered observable behavior —
// confirm the new behavior is intended before updating assertions.

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LMS.Tests.Integration.Infrastructure;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Integration tests for F-04 — Leave Type &amp; Policy Management.
/// Tests exercise the full ASP.NET Core pipeline: middleware → controller → response.
/// JWT validation is provided by <see cref="TestAuthHandler"/>; all other infrastructure
/// (routing, authorization middleware, filters) is real.
///
/// IT- IDs covered: IT-F04-001, IT-F04-002, IT-F04-003, IT-F04-004, IT-F04-005, IT-F04-006, IT-F04-007.
/// </summary>
[Collection(nameof(LmsIntegrationCollection))]
public class LeavePolicyIntegrationTests : IClassFixture<LmsWebApplicationFactory>
{
    private readonly LmsWebApplicationFactory _factory;

    public LeavePolicyIntegrationTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-001: GET /api/leave-types returns 200 with { data: [...] }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-001: Any authenticated user can retrieve the list of leave types.
    /// The response must be wrapped in the ApiResponse&lt;T&gt; envelope with a "data" property.
    /// An empty in-memory database means data will be an empty collection, not null.
    /// </summary>
    [Fact]
    public async Task IT_F04_001_GET_LeaveTypes_Returns200_WithDataEnvelope()
    {
        // Arrange — authenticated as Employee (minimum role; endpoint has no role restriction).
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.Employee);

        // Act
        var response = await client.GetAsync("/api/leave-types");

        // Assert — HTTP 200 OK
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-types must return 200 for any authenticated user");

        // Assert — response body has the ApiResponse<T> { data: [...] } envelope
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must be present; an empty list is valid but null is not");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-002: POST /api/leave-types with HRAdmin creates leave type → 201 + { data: LeaveTypeDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-002: An HRAdmin can create a new leave type via POST /api/leave-types.
    /// The response must be 201 Created and the body must carry the created LeaveTypeDto
    /// inside the ApiResponse&lt;T&gt; envelope.
    /// </summary>
    [Fact]
    public async Task IT_F04_002_POST_LeaveTypes_AsHRAdmin_Creates_Returns201_WithDataEnvelope()
    {
        // Arrange — authenticated as HRAdmin (required by [Authorize(Policy="HRAdminOrSuperAdmin")]).
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        var payload = new
        {
            name = "Sick Leave",
            code = "SL",
            annualDays = 12,
            requiresAttachment = false,
            requiresHrApproval = false,
            description = "Leave for medical illness"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-types", payload);

        // Assert — HTTP 201 Created
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "POST /api/leave-types with valid payload and HRAdmin role must return 201 Created");

        // Assert — response body has the ApiResponse<T> envelope with data containing the created leave type
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must contain the newly created LeaveTypeDto");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-003: POST /api/leave-types with Employee role returns 403
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-003: An Employee cannot create leave types — the endpoint requires
    /// HRAdmin or SuperAdmin. The authorization middleware must return 403 Forbidden
    /// before any business logic runs.
    /// </summary>
    [Fact]
    public async Task IT_F04_003_POST_LeaveTypes_AsEmployee_Returns403()
    {
        // Arrange — authenticated as Employee only.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.Employee);

        var payload = new
        {
            name = "Annual Leave",
            code = "AL",
            annualDays = 20,
            requiresAttachment = false,
            requiresHrApproval = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-types", payload);

        // Assert — HTTP 403 Forbidden (not 401; the user IS authenticated, just not authorized)
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "an Employee role must not be permitted to create leave types; " +
            "RBAC must return 403 after authentication succeeds");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-004: PUT /api/leave-types/{id} updates → 200 + { data: LeaveTypeDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-004: An HRAdmin can update an existing leave type via PUT /api/leave-types/{id}.
    /// The test first creates a leave type, then updates it.
    /// The response must be 200 OK with the updated LeaveTypeDto in the ApiResponse&lt;T&gt; envelope.
    /// </summary>
    [Fact]
    public async Task IT_F04_004_PUT_LeaveType_AsHRAdmin_Updates_Returns200_WithDataEnvelope()
    {
        // Arrange — authenticated as HRAdmin.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Step 1: Create a leave type to update.
        var createPayload = new
        {
            name = "Casual Leave",
            code = "CL",
            annualDays = 10,
            requiresAttachment = false,
            requiresHrApproval = false,
            description = "Casual leave for personal matters"
        };
        var createResponse = await client.PostAsJsonAsync("/api/leave-types", createPayload);
        createResponse.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "prerequisite: leave type must be created successfully before the update test can run");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiResponseWithId>();
        createEnvelope.Should().NotBeNull();
        var createdId = createEnvelope!.Data?.Id;
        createdId.Should().NotBeNull("the created leave type must have an Id so we can update it");

        // Step 2: Update the leave type.
        var updatePayload = new
        {
            name = "Casual Leave Updated",
            annualDays = 15,
            requiresAttachment = true,
            requiresHrApproval = false,
            description = "Updated casual leave description",
            isActive = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/leave-types/{createdId}", updatePayload);

        // Assert — HTTP 200 OK
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "PUT /api/leave-types/{id} with valid payload and HRAdmin role must return 200 OK");

        // Assert — response body has the ApiResponse<T> envelope
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must contain the updated LeaveTypeDto");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-005: DELETE /api/leave-types/{id} soft-deletes → deleted record not in GET list
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-005: An HRAdmin can soft-delete a leave type via DELETE /api/leave-types/{id}.
    /// After deletion the leave type must no longer appear in the GET /api/leave-types list.
    /// Soft-delete sets IsActive=false rather than removing the record from the database.
    ///
    /// Note: As of the date of this test, the DELETE endpoint is specified in the F-04 feature spec.
    /// If this test returns 404 or 405 for the DELETE call, the endpoint has not yet been implemented
    /// in LeavePolicyController.cs — add [HttpDelete("leave-types/{id:guid}")] there.
    /// </summary>
    [Fact]
    public async Task IT_F04_005_DELETE_LeaveType_AsHRAdmin_SoftDeletes_NotInGetList()
    {
        // Arrange — authenticated as HRAdmin.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Step 1: Create a leave type to delete.
        var createPayload = new
        {
            name = "Maternity Leave",
            code = "ML",
            annualDays = 90,
            requiresAttachment = true,
            requiresHrApproval = true,
            description = "Maternity leave"
        };
        var createResponse = await client.PostAsJsonAsync("/api/leave-types", createPayload);
        createResponse.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "prerequisite: leave type must be created successfully before the delete test can run");

        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiResponseWithId>();
        createEnvelope.Should().NotBeNull();
        var createdId = createEnvelope!.Data?.Id;
        createdId.Should().NotBeNull("the created leave type must have an Id so we can delete it");

        // Step 2: Soft-delete the leave type.
        // Act
        var deleteResponse = await client.DeleteAsync($"/api/leave-types/{createdId}");

        // Assert — HTTP 200 OK or 204 No Content (soft-delete; the record still exists, just deactivated)
        deleteResponse.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "DELETE /api/leave-types/{id} must return 200 or 204 to confirm the soft-delete succeeded");

        // Step 3: Verify the deleted leave type is no longer returned in GET /api/leave-types.
        var listResponse = await client.GetAsync("/api/leave-types");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ApiResponseListShape>();
        listEnvelope.Should().NotBeNull();

        // The soft-deleted leave type's Id must not appear in the active list.
        var ids = listEnvelope!.Data?
            .Select(lt => lt.Id)
            .ToList() ?? new List<Guid>();

        ids.Should().NotContain(
            createdId!.Value,
            "a soft-deleted leave type must not appear in the active GET /api/leave-types list");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-006: GET /api/leave-policies returns 200 with { data: [...] }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-006: Any authenticated user can retrieve leave policies.
    /// This test verifies the collection endpoint returns 200 and wraps the result
    /// in the ApiResponse&lt;T&gt; envelope.
    ///
    /// The F-04 spec exposes policies scoped to a leave type:
    ///   GET /api/leave-types/{leaveTypeId}/policies
    /// This test creates a leave type first, then queries its policies,
    /// confirming the full DB → service → API pipeline is wired correctly.
    /// </summary>
    [Fact]
    public async Task IT_F04_006_GET_LeavePolicies_Returns200_WithDataEnvelope()
    {
        // Arrange — authenticated as HRAdmin (needed for setup POST; GET is accessible to all).
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Step 1: Create a leave type to scope policy queries to.
        var createTypePayload = new
        {
            name = "Paternity Leave",
            code = "PL",
            annualDays = 10,
            requiresAttachment = false,
            requiresHrApproval = false,
            description = "Paternity leave"
        };
        var createTypeResponse = await client.PostAsJsonAsync("/api/leave-types", createTypePayload);
        createTypeResponse.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "prerequisite: leave type must be created so we can query its policies");

        var createTypeEnvelope = await createTypeResponse.Content.ReadFromJsonAsync<ApiResponseWithId>();
        var leaveTypeId = createTypeEnvelope!.Data?.Id;
        leaveTypeId.Should().NotBeNull("the created leave type must have an Id for policy queries");

        // Act — GET /api/leave-types/{leaveTypeId}/policies
        var response = await client.GetAsync($"/api/leave-types/{leaveTypeId}/policies");

        // Assert — HTTP 200 OK
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "GET /api/leave-types/{id}/policies must return 200 for any authenticated user");

        // Assert — response body has the ApiResponse<T> { data: [...] } envelope
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must be present; an empty list is valid but null is not");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IT-F04-007: POST /api/leave-policies creates policy → 201 + { data: LeavePolicyDto }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT-F04-007: An HRAdmin can create a leave policy via POST /api/leave-policies.
    /// The test first creates the required leave type, then creates a policy linked to it.
    /// The response must be 201 Created with the created LeavePolicyDto in the ApiResponse&lt;T&gt; envelope.
    /// </summary>
    [Fact]
    public async Task IT_F04_007_POST_LeavePolicies_AsHRAdmin_Creates_Returns201_WithDataEnvelope()
    {
        // Arrange — authenticated as HRAdmin.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthScheme.RoleHeader, RoleNames.HRAdmin);

        // Step 1: Create a leave type to associate the policy with.
        var createTypePayload = new
        {
            name = "Earned Leave",
            code = "EL",
            annualDays = 21,
            requiresAttachment = false,
            requiresHrApproval = false,
            description = "Earned / privilege leave"
        };
        var createTypeResponse = await client.PostAsJsonAsync("/api/leave-types", createTypePayload);
        createTypeResponse.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "prerequisite: leave type must be created before a policy can reference it");

        var createTypeEnvelope = await createTypeResponse.Content.ReadFromJsonAsync<ApiResponseWithId>();
        var leaveTypeId = createTypeEnvelope!.Data?.Id;
        leaveTypeId.Should().NotBeNull("the created leave type must have an Id for the policy");

        // Step 2: Create a leave policy linked to that leave type.
        var createPolicyPayload = new
        {
            leaveTypeId = leaveTypeId!.Value,
            name = "Standard Earned Leave Policy",
            annualAllotment = 21,
            maxCarryForward = 5,
            maxConsecutiveDays = 10,
            minNoticeDays = 2,
            accrualMonthly = true,
            accrualRate = 1.75m,
            isActive = true,
            effectiveFrom = DateTime.UtcNow.Date
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/leave-policies", createPolicyPayload);

        // Assert — HTTP 201 Created
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "POST /api/leave-policies with valid payload and HRAdmin role must return 201 Created");

        // Assert — response body has the ApiResponse<T> envelope with data containing the created policy
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseShape>();
        envelope.Should().NotBeNull("the response must be wrapped in ApiResponse<T>");
        envelope!.Data.Should().NotBeNull(
            "response.body.data must contain the newly created LeavePolicyDto");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper types — minimal POCOs for deserializing ApiResponse<T> envelopes
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal POCO for deserializing the <c>{ "data": ... }</c> envelope when we only
    /// need to assert that the property is present and non-null.
    /// </summary>
    private sealed class ApiResponseShape
    {
        public object? Data { get; set; }
    }

    /// <summary>
    /// POCO for deserializing <c>{ "data": { "id": "..." } }</c> — used when we need
    /// the Id of the created resource to reference it in follow-up requests.
    /// </summary>
    private sealed class ApiResponseWithId
    {
        public LeaveTypeIdShape? Data { get; set; }
    }

    private sealed class LeaveTypeIdShape
    {
        public Guid? Id { get; set; }
    }

    /// <summary>
    /// POCO for deserializing <c>{ "data": [ { "id": "..." }, ... ] }</c> — used in
    /// IT-F04-005 to confirm a deleted leave type no longer appears in the GET list.
    /// </summary>
    private sealed class ApiResponseListShape
    {
        public List<LeaveTypeIdShape>? Data { get; set; }
    }
}
