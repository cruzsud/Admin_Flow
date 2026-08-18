using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace AdminFlow.Budget.IntegrationTests;

public sealed class TechnicalEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public TechnicalEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            ;
        _client = _factory
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public void Observability_WhenApplicationStarts_ShouldRegisterTraceAndMetricProviders()
    {
        Assert.NotNull(_factory.Services.GetService<TracerProvider>());
        Assert.NotNull(_factory.Services.GetService<MeterProvider>());
    }

    [Fact]
    public async Task Health_WhenApplicationIsRunning_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SwaggerDocument_InDevelopment_ShouldBeAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AdminFlow.Budget API", content);
    }

    [Fact]
    public async Task SwaggerUi_InDevelopment_ShouldBeAvailable()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

}
