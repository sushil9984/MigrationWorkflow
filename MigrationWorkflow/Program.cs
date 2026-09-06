using Microsoft.EntityFrameworkCore;
using MigrationWorkflow.Application.Agents;
using MigrationWorkflow.Application.Workflow;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Infrastructure.Data;
using MigrationWorkflow.Infrastructure.Repositories;
using MigrationWorkflow.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "migration_db";

builder.Services.AddSingleton(sp => 
    new MongoDbContext(mongoConnectionString, mongoDatabaseName));

// Configure PostgreSQL
var postgresConnectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? "Host=localhost;Database=migration_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<PostgresDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// Register Repositories
builder.Services.AddScoped<IPartyRepository, PartyRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Register Agents
builder.Services.AddScoped<IMigrationAgent, MigrationAgent>();
builder.Services.AddScoped<IReconciliationAgent, ReconciliationAgent>();
builder.Services.AddScoped<IAnalysisNarrativeService, SemanticKernelAnalysisNarrativeService>();
builder.Services.AddScoped<IAnalysisAgent, AnalysisAgent>();

// Register Workflow Orchestrator
builder.Services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var postgresContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
    await postgresContext.Database.EnsureCreatedAsync();
    app.Logger.LogInformation("PostgreSQL database initialized");
}

app.Logger.LogInformation("Migration Workflow API is ready!");
app.Logger.LogInformation("MongoDB: {MongoConnection}", mongoConnectionString);
app.Logger.LogInformation("PostgreSQL: {PostgresConnection}", postgresConnectionString);

app.Run();
