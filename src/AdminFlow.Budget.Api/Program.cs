using AdminFlow.Budget.Api.Observability;
using AdminFlow.Budget.Infrastructure;
using AdminFlow.Budget.Infrastructure.Messaging;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var connectionString = builder.Configuration.GetConnectionString("BudgetDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'BudgetDatabase' is not configured.");

var rabbitMqOptions = new RabbitMqOptions
{
    Enabled = builder.Configuration.GetValue<bool>("RabbitMq:Enabled"),
    HostName = builder.Configuration["RabbitMq:HostName"] ?? "localhost",
    Port = builder.Configuration.GetValue("RabbitMq:Port", 5672),
    UserName = builder.Configuration["RabbitMq:UserName"] ?? string.Empty,
    Password = builder.Configuration["RabbitMq:Password"] ?? string.Empty,
    VirtualHost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
    MaxRetryAttempts = builder.Configuration.GetValue("RabbitMq:MaxRetryAttempts", 3),
    RetryDelayMilliseconds = builder.Configuration.GetValue(
        "RabbitMq:RetryDelayMilliseconds",
        5_000)
};

builder.Services.AddInfrastructure(connectionString, rabbitMqOptions);
builder.Services.AddAdminFlowOpenTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AdminFlow.Budget API",
        Version = "v1",
        Description = "API para gestão orçamentária e aprovação de despesas."
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
