using System.Text;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using LeilaoAuto.Api.Authorization;
using LeilaoAuto.Application.Common;
using LeilaoAuto.Api.Health;
using LeilaoAuto.Application;
using LeilaoAuto.Application.Validators;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Infrastructure;
using LeilaoAuto.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy("API running"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LEILAOAUTO API",
        Version = "v1",
        Description = "LEILAOAUTO backend API (Phase 10): auth, monitoring, lots, analytics, billing and connectors."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? builder.Configuration["Cors:AllowedOrigins"]?
                         .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                     ?? ["http://localhost:4200", "http://127.0.0.1:4200"];
var normalizedAllowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        if (normalizedAllowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.SetIsOriginAllowed(origin =>
            {
                if (normalizedAllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var parsedOrigin))
                {
                    return false;
                }

                if (parsedOrigin.Scheme.Equals("chrome-extension", StringComparison.OrdinalIgnoreCase)
                    || parsedOrigin.Scheme.Equals("moz-extension", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Allow localhost/loopback origins in dev scenarios (any local port).
                return parsedOrigin.IsLoopback;
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, MinimumPlanHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PlanPolicies.ProOrHigher, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new MinimumPlanRequirement(PlanType.Pro)));

    options.AddPolicy(PlanPolicies.PremiumOrHigher, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new MinimumPlanRequirement(PlanType.Premium)));

    options.AddPolicy(PlanPolicies.EliteOnly, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new MinimumPlanRequirement(PlanType.Elite)));
});

var app = builder.Build();

app.Logger.LogInformation(
    "Configured CORS origins: {Origins}. Loopback origins are allowed.",
    string.Join(", ", normalizedAllowedOrigins));

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }
    };
});

var useHttpsRedirection = app.Configuration.GetValue<bool?>("Http:UseHttpsRedirection") ?? !app.Environment.IsDevelopment();
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception.");

        var (statusCode, title) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainRuleException => (StatusCodes.Status400BadRequest, "Business rule violation"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Invalid operation"),
            NotSupportedException => (StatusCodes.Status501NotImplemented, "Not implemented"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= 500 ? "An unexpected error occurred while processing the request." : exception.Message
        };

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.ErrorMessage).Distinct().ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}
