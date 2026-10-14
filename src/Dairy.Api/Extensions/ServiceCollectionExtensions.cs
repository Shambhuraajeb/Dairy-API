using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dairy.Api.Authorization;
using Dairy.Api.Middleware;
using Dairy.Application.Common;
using Dairy.Application.Interfaces;
using Dairy.Domain.Constants;
using Dairy.Infrastructure.Data;
using Dairy.Infrastructure.Data.ConnectionFactory;
using Dairy.Infrastructure.Files;
using Dairy.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Dairy.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "DairyCors";

    /// <summary>Registers everything. Feature agents never edit this: services/repositories/validators are auto-registered.</summary>
    public static IServiceCollection AddDairy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDairyApplication();
        services.AddDairyInfrastructure(configuration);
        services.AddDairyAuthentication(configuration);
        services.AddDairyApi();
        services.AddDairyCors(configuration);
        services.AddDairySwagger();
        return services;
    }

    /// <summary>Every non-abstract class named *Service in Application (interface I&lt;Name&gt;) + all FluentValidation validators.</summary>
    public static IServiceCollection AddDairyApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApiError).Assembly;
        RegisterByConvention(services, assembly, "Service");
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);
        return services;
    }

    /// <summary>Connection factory, security helpers, and every non-abstract *Repository in Infrastructure (interface I&lt;Name&gt;).</summary>
    public static IServiceCollection AddDairyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DapperConfig.Initialize();

        services.AddHttpContextAccessor();
        services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton(JwtOptions.FromConfiguration(configuration));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuditService, AuditService>();

        RegisterByConvention(services, typeof(DapperConfig).Assembly, "Repository");
        return services;
    }

    public static IServiceCollection AddDairyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = JwtOptions.FromConfiguration(configuration);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await ApiResponseWriter.WriteFailureAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "Authentication is required or your session has expired. Please sign in again.");
                    },
                    OnForbidden = context => ApiResponseWriter.WriteFailureAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "You do not have permission to perform this action.")
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddDairyApi(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.Filters.Add<ValidationActionFilter>();
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value is { Errors.Count: > 0 })
                        .SelectMany(e => e.Value!.Errors.Select(err => new ApiError(
                            FieldNames.ToCamelCase(e.Key),
                            string.IsNullOrWhiteSpace(err.ErrorMessage) ? "The value is invalid." : err.ErrorMessage)))
                        .ToList();

                    return new ObjectResult(ApiResponse<object?>.Fail("Validation failed.", errors))
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                };
            });

        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
                                    HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        return services;
    }

    public static IServiceCollection AddDairyCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
            origins = new[] { "http://localhost:4200" };

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Content-Disposition")));
        return services;
    }

    public static IServiceCollection AddDairySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Dairy Management API", Version = "v1" });
            options.CustomSchemaIds(SchemaId);

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Paste the JWT access token (without the 'Bearer ' prefix).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            options.AddSecurityDefinition("Bearer", scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
        });
        return services;
    }

    /// <summary>Full name (two DTOs share a short name across modules); generic types get a readable id without assembly details.</summary>
    private static string SchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return name + "Of" + string.Join("And", type.GetGenericArguments().Select(SchemaId));
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static void RegisterByConvention(IServiceCollection services, Assembly assembly, string suffix)
    {
        var types = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false, IsNested: false }
                        && t.Name.EndsWith(suffix, StringComparison.Ordinal));

        foreach (var implementation in types)
        {
            var contract = implementation.GetInterfaces().FirstOrDefault(i => i.Name == "I" + implementation.Name);
            if (contract is not null)
                services.AddScoped(contract, implementation);
        }
    }
}
