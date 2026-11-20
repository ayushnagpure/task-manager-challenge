using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using TaskManager.Api.Data;
using TaskManager.Api.Services;

namespace TaskManager.Api.Infrastructure;

/// <summary>Registration helpers for cross-cutting concerns.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MVC controllers with the API's JSON and validation conventions:
    /// enums as strings (numbers rejected), no parser/exception details in 400 responses,
    /// only explicit <c>[Required]</c> attributes (no implicit "field is required" errors), and
    /// validation error keys named like the JSON body (camelCase, e.g. <c>title</c>).
    /// </summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
                options.AllowInputFormatterExceptionMessages = false;
            });
        return services;
    }

    /// <summary>Registers application services and the system <see cref="TimeProvider"/>.</summary>
    public static IServiceCollection AddTaskServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITaskService, TaskService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="TaskDbContext"/> on SQL Server. The connection string comes from
    /// <c>ConnectionStrings:DefaultConnection</c> and is validated at startup (fails fast if missing).
    /// </summary>
    public static IServiceCollection AddTaskDatabase(this IServiceCollection services)
    {
        services.AddOptions<DatabaseSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
                settings.ConnectionString = configuration.GetConnectionString(DatabaseSettings.ConnectionStringName) ?? string.Empty)
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ConnectionString), DatabaseSettings.MissingConnectionStringMessage)
            .ValidateOnStart();

        services.AddDbContext<TaskDbContext>((serviceProvider, options) =>
            options.UseSqlServer(serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value.ConnectionString));

        return services;
    }

    /// <summary>
    /// Registers ProblemDetails (with <c>traceId</c>/<c>instance</c> on every response) and the
    /// <see cref="GlobalExceptionHandler"/>.
    /// </summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                ProblemDetailsEnricher.Enrich(context.ProblemDetails, context.HttpContext));
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    /// <summary>
    /// Binds and validates <see cref="CorsSettings"/> (fails at startup if invalid) and registers
    /// the <see cref="CorsSettings.PolicyName"/> policy.
    /// </summary>
    public static IServiceCollection AddUiCors(this IServiceCollection services)
    {
        services.AddOptions<CorsSettings>()
            .BindConfiguration(CorsSettings.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorsSettings>, CorsSettingsValidator>();
        services.AddCors();
        services.AddSingleton<IConfigureOptions<CorsOptions>, ConfigureCorsOptions>();
        return services;
    }

    /// <summary>
    /// Registers HTTP request logging limited to method, path, status code and duration.
    /// Headers and bodies are deliberately not logged.
    /// </summary>
    public static IServiceCollection AddRequestLogging(this IServiceCollection services) =>
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });
}
