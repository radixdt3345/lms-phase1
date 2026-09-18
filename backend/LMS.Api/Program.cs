using Hangfire;
using Hangfire.PostgreSql;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Seed;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
builder.Services.AddScoped<ISeedService, SeedService>();          // F-14: Initial Data Seeding API
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();  // F-05: Leave Balance Management
builder.Services.AddScoped<LMS.Infrastructure.Seed.DataSeeder>();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Hangfire Dashboard (development only — no auth in dev) ────────────────────
// NOTE: For production, add IAuthorizationFilter to restrict dashboard access.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.Run();

public partial class Program { }
