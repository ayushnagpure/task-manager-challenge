using System.Text.Json.Serialization;
using TaskManager.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApiProblemDetails();
builder.Services.AddUiCors();
builder.Services.AddTaskDatabase();
builder.Services.AddRequestLogging();

var app = builder.Build();

// Outermost: log every request with its final status code (including handled 500s).
app.UseHttpLogging();

// Before the exception handler so CORS headers are also present on error responses,
// letting the browser UI read the ProblemDetails body.
app.UseCors(CorsSettings.PolicyName);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

/// <summary>Application entry point. Declared partial so integration tests can use it with WebApplicationFactory.</summary>
public partial class Program;
