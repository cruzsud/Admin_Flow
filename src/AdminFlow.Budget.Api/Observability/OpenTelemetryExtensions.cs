using AdminFlow.Budget.Infrastructure.Observability;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AdminFlow.Budget.Api.Observability;

internal static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAdminFlowOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"]
            ?? "AdminFlow.Budget.Api";
        var samplingRatio = configuration.GetValue(
            "OpenTelemetry:Tracing:SamplingRatio",
            1.0d);

        if (samplingRatio is < 0 or > 1)
        {
            throw new InvalidOperationException(
                "OpenTelemetry tracing sampling ratio must be between 0 and 1.");
        }

        var consoleEnabled = configuration.GetValue<bool>(
            "OpenTelemetry:Exporters:Console:Enabled");
        var otlpEnabled = configuration.GetValue<bool>(
            "OpenTelemetry:Exporters:Otlp:Enabled");
        var otlpEndpoint = GetOtlpEndpoint(configuration, otlpEnabled);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString())
                .AddAttributes(
                [
                    new KeyValuePair<string, object>(
                        "deployment.environment.name",
                        environment.EnvironmentName)
                ]))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(samplingRatio)))
                    .AddSource(BudgetTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.RecordException = true)
                    .AddHttpClientInstrumentation(options =>
                        options.RecordException = true)
                    .AddNpgsql();

                if (consoleEnabled)
                {
                    tracing.AddConsoleExporter();
                }

                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(BudgetTelemetry.MeterName)
                    .AddNpgsqlInstrumentation(_ => { })
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (consoleEnabled)
                {
                    metrics.AddConsoleExporter();
                }

                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
                }
            });

        return services;
    }

    private static Uri? GetOtlpEndpoint(
        IConfiguration configuration,
        bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        var configuredEndpoint = configuration[
            "OpenTelemetry:Exporters:Otlp:Endpoint"];
        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "OpenTelemetry OTLP endpoint must be an absolute HTTP or HTTPS URI.");
        }

        if (endpoint.Scheme == Uri.UriSchemeHttp && !endpoint.IsLoopback)
        {
            throw new InvalidOperationException(
                "OpenTelemetry OTLP endpoint must use HTTPS when it is not local.");
        }

        return endpoint;
    }
}
