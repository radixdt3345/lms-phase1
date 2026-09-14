# API Contract Registry — LMS Phase 1

All endpoints return `ApiResponse<T>` envelopes: `{ "data": T }`.
Error responses use ProblemDetails RFC 7807 with a stable `error_code` extension field.

---

## F-01 Auth Endpoints

Base path: `/api/auth`

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | /api/auth/me | Bearer (any role) | `ApiResponse<UserProfileDto>` |
| GET | /api/auth/users | Bearer (HRAdmin, SuperAdmin) | `ApiResponse<IEnumerable<UserProfileDto>>` |
| POST | /api/auth/users/{userId}/roles | Bearer (HRAdmin, SuperAdmin) | `ApiResponse<UserProfileDto>` |
| DELETE | /api/auth/users/{userId}/roles/{roleId} | Bearer (HRAdmin, SuperAdmin) | `ApiResponse<UserProfileDto>` |
| GET | /api/auth/roles | Bearer (any role) | `ApiResponse<IEnumerable<RoleDto>>` |

### DTOs

**UserProfileDto**
```json
{
  "id": "uuid",
  "azureAdObjectId": "string",
  "email": "string",
  "displayName": "string",
  "employeeCode": "string|null",
  "department": "string|null",
  "roles": ["string"],
  "isActive": true,
  "status": "Active|Locked|Inactive"
}
```

**RoleDto**
```json
{
  "id": "uuid",
  "name": "string",
  "description": "string|null"
}
```

**UserRoleAssignmentDto** (request body for POST .../roles)
```json
{
  "userId": "uuid",
  "roleId": "uuid"
}
```
