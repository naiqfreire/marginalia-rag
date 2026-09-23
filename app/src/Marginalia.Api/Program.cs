using System.Globalization;
using FluentValidation;
using Marginalia.Api.Common;
using Marginalia.Api.Features.IngestPage;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Marginalia API starting");

    ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .Enrich.FromLogContext());

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    builder.Services.AddOpenApi();

    builder.Services.Configure<IngestPageLimits>(options =>
    {
        var max = builder.Configuration.GetValue<int?>("MAX_PAGE_WORDS") ?? 10_000;
        options.MaxPageWords = max;
    });
    builder.Services.AddSingleton<IValidator<IngestPageRequest>, IngestPageValidator>();

    var app = builder.Build();

    app.UseExceptionHandler(errorHandler =>
    {
        errorHandler.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("ExceptionHandler");

            if (exception is BadHttpRequestException badRequest)
            {
                logger.LogWarning(
                    "Bad request for {Method} {Path}: {Reason}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    badRequest.Message);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new ErrorResponse("Invalid request body", "INVALID_BODY"));
                return;
            }

            if (exception is not null)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path.Value);
            }
            else
            {
                logger.LogError(
                    "Unhandled exception for {Method} {Path} (no exception detail)",
                    context.Request.Method,
                    context.Request.Path.Value);
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse("Internal server error", "INTERNAL_ERROR"));
        });
    });

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (_, elapsed, ex) =>
            ex is null ? Serilog.Events.LogEventLevel.Information
                       : Serilog.Events.LogEventLevel.Error;
        options.EnrichDiagnosticContext = (_, httpContext) =>
        {
            // Metadata only — never query strings, bodies, or headers with secrets
            httpContext.Items["RequestPath"] = httpContext.Request.Path.Value;
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    app.MapIngestPage();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Marginalia API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
