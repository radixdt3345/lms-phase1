using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Seed;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Connection string MUST come from an environment variable — never hardcoded.
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("DATABASE_URL environment variable is not set.");

builder.Services.AddDbContext<LmsDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Hangfire — Background Jobs (F-15) ─────────────────────────────────────────
// Hangfire creates its own schema tables (hangfire.job, hangfire.state, etc.)
// automatically when the PostgreSQL storage provider initialises on first startup.
// No EF migration is required for those tables.
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opts =>
        opts.UseNpgsqlConnection(connectionString)));

// Register Hangfire server with default worker count (Environment.ProcessorCount * 5)
builder.Services.AddHangfireServer();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ILeavePolicyService, LeavePolicyService>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ICompOffService, CompOffService>();    // F-07: Comp-Off Management
builder.Services.AddScoped<ISeedService, SeedService>();          // F-14: Initial Data Seeding API
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();  // F-05: Leave Balance Management
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();  // F-06: Leave Application & Workflow
builder.Services.AddScoped<LMS.Infrastructure.Seed.DataSeeder>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();  // F-08: Approval Workflow
builder.Services.AddScoped<IDashboardService, DashboardService>();  // F-11: Dashboards

// F-09: Notifications & Email
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// F-15: Background Jobs (Hangfire)
builder.Services.AddScoped<IJobSchedulerService, JobSchedulerService>();
builder.Services.AddScoped<IReportService, ReportService>();      // F-12: Reports & CSV Export

// ── Authentication — Azure AD JWT Bearer ─────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("AZURE_AD_AUTHORITY");
        options.Audience = Environment.GetEnvironmentVariable("AZURE_AD_AUDIENCE");
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Azure AD emits roles in the "roles" claim; map it so [Authorize(Roles=...)] works.
            RoleClaimType = "roles"
        };
    });

// ── Authorisation — role-based policies ──────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HRAdminOrSuperAdmin", policy =>
        policy.RequireRole("HRAdmin", "SuperAdmin"));
});

// ── Health Checks (W-001) ─────────────────────────────────────────────────────
// /health — liveness probe: is the process up?
// /ready  — readiness probe: is the DB reachable and the service fully initialised?
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" });

// ── Rate Limiting (W-002) ─────────────────────────────────────────────────────
// Fixed-window policy: 100 requests per minute per client IP on write/auth endpoints.
// Read-heavy endpoints (GET) are exempt to keep dashboard performance snappy.
// Adjust limits via environment variables for staging vs production.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // "auth" policy — tighter limit for login/token endpoints (prevent brute-force)
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // "api-write" policy — POST/PUT/PATCH/DELETE endpoints
    options.AddFixedWindowLimiter("api-write", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

// ── Controllers & API explorer ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LMS API",
        Version = "v1",
        Description = "Leave Management System — Phase 1"
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Paste the JWT Bearer token here",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

// ── Build pipeline ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Rate limiting middleware must come before auth so rejections are cheap
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health endpoints (W-001) ───────────────────────────────────────────────────
// /health — liveness: returns 200 when the process is up (no DB check)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,   // exclude all named checks — pure liveness
    ResponseWriter = async (ctx, _) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{\"status\":\"healthy\"}");
    }
});

// /ready — readiness: returns 200 only when DB is reachable
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var status = report.Status == HealthStatus.Healthy ? "ready" : "not_ready";
        await ctx.Response.WriteAsync($"{{\"status\":\"{status}\"}}");
    }
});

// ── Hangfire Dashboard (W-003) ────────────────────────────────────────────────
// Development: open access for local debugging.
// Production:  restricted to SuperAdmin role via DashboardAuthorizationFilter.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
else
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireSuperAdminAuthorizationFilter() }
    });
}

// ── F-15: Register recurring Hangfire jobs on startup ─────────────────────────
// JobSchedulerService.RegisterJobs() is idempotent — Hangfire's AddOrUpdate
// overwrites existing registrations, so this is safe to call on every restart.
using (var scope = app.Services.CreateScope())
{
    var jobScheduler = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();
    jobScheduler.RegisterJobs();
}

app.Run();

public partial class Program { }

// ── Hangfire dashboard auth filter (W-003) ────────────────────────────────────
/// <summary>
/// Restricts the Hangfire dashboard to users with the SuperAdmin role.
/// The request must carry a valid Azure AD JWT bearing the "SuperAdmin" role claim.
/// </summary>
internal sealed class HangfireSuperAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Must be authenticated
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return false;

        // Must carry SuperAdmin role
        return httpContext.User.IsInRole("SuperAdmin");
    }
}
