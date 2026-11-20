using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;

namespace TaskManager.Api.Infrastructure;

/// <summary>Registration helpers for cross-cutting concerns.</summary>
public static class ServiceCollectionExtensions
{
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
