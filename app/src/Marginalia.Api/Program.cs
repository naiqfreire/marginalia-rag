using MediatR;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Marginalia API starting");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    builder.Services.AddOpenApi();

    var app = builder.Build();

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
