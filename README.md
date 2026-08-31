# Migration Workflow - Multi-Agent System

A .NET 10.0 multi-agent workflow system for migrating data from MongoDB to PostgreSQL with automated reconciliation and analysis.

## Architecture

The solution follows Clean Architecture principles with four projects:

### 1. **MigrationWorkflow.Domain**
- Contains core business entities, interfaces, and models
- No dependencies on other layers
- Defines:
  - Entity models (`Party`, `Customer`)
  - Agent interfaces (`IAgent`, `IMigrationAgent`, `IReconciliationAgent`, `IAnalysisAgent`)
  - Repository interfaces (`IPartyRepository`, `ICustomerRepository`)
  - Workflow models and enums

### 2. **MigrationWorkflow.Application**
- Contains business logic and agent implementations
- Depends only on Domain layer
- Implements:
  - **MigrationAgent**: Migrates data from MongoDB Party collection to PostgreSQL Customer table
  - **ReconciliationAgent**: Generates reconciliation reports comparing source and target data
  - **AnalysisAgent**: Analyzes reconciliation reports and provides recommendations
  - **WorkflowOrchestrator**: Coordinates the execution of all agents in sequence

### 3. **MigrationWorkflow.Infrastructure**
- Contains data access implementations
- Depends on Domain and Application layers
- Implements:
  - `MongoDbContext`: MongoDB connection and Party collection access
  - `PostgresDbContext`: PostgreSQL EF Core context for Customer entities
  - `PartyRepository`: Repository for Party data access
  - `CustomerRepository`: Repository for Customer data access

### 4. **MigrationWorkflow** (Web API)
- ASP.NET Core Web API
- Exposes REST endpoints for workflow execution
- Handles dependency injection and configuration

## Agent Workflow

### Agent 1: Migration Agent
**Purpose**: Migrates data from MongoDB Party collection to PostgreSQL Customer table

**Features**:
- Batch processing for large datasets
- Date range filtering
- Upsert logic (insert new or update existing records)
- Error tracking per record
- Progress reporting

**Mapping**:
```
Party (MongoDB)         →  Customer (PostgreSQL)
├── PartyId            →  CustomerId
├── Name               →  Name
├── Type               →  CustomerType
├── Email              →  Email
├── Phone              →  Phone
├── Address.Street     →  Street
├── Address.City       →  City
├── Address.State      →  State
├── Address.ZipCode    →  ZipCode
├── Address.Country    →  Country
├── Status             →  Status
├── CreatedDate        →  CreatedDate
├── ModifiedDate       →  ModifiedDate
└── _id                →  SourceId
```

### Agent 2: Reconciliation Agent
**Purpose**: Generates detailed reconciliation reports

**Features**:
- Record count comparison
- Field-level data validation
- Discrepancy detection (Missing, Mismatch, Extra)
- Data accuracy percentage calculation
- Summary report generation

### Agent 3: Analysis Agent
**Purpose**: Analyzes reconciliation reports and provides insights

**Features**:
- Quality score calculation (0-100%)
- Overall status determination (Success/Warning/Failed)
- Key findings identification
- Critical issues flagging
- Actionable recommendations
- Detailed analysis report generation

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK** or later
2. **MongoDB** (localhost:27017 or remote instance)
3. **PostgreSQL** (localhost:5432 or remote instance)

### Configuration

Update connection strings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "PostgreSQL": "Host=localhost;Database=migration_db;Username=postgres;Password=yourpassword"
  },
  "MongoDB": {
    "DatabaseName": "migration_db"
  }
}
```

### Running the Application

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Run the API**:
   ```bash
   cd MigrationWorkflow
   dotnet run
   ```

3. **Access the API**:
   - API: `https://localhost:5001` or `http://localhost:5000`
   - OpenAPI/Swagger: `https://localhost:5001/openapi/v1.json`

## API Endpoints

### 1. Start Workflow (Full Control)
```http
POST /api/workflow/start
Content-Type: application/json

{
  "workflowId": "optional-custom-id",
  "migrationRequest": {
    "collectionName": "Party",
    "targetTableName": "Customer",
    "batchSize": 100,
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-12-31T23:59:59Z"
  },
  "generateReconciliationReport": true,
  "performAnalysis": true
}
```

**Response**:
```json
{
  "workflowId": "generated-guid",
  "success": true,
  "currentStage": "Completed",
  "completedStage": "Completed",
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:05:30Z",
  "totalDuration": "00:05:30",
  "migrationResult": {
    "success": true,
    "totalRecordsProcessed": 1000,
    "successfulMigrations": 998,
    "failedMigrations": 2,
    "duration": "00:03:20",
    "errors": []
  },
  "reconciliationReport": {
    "sourceRecordCount": 1000,
    "targetRecordCount": 998,
    "mismatchCount": 5,
    "dataAccuracyPercentage": 99.5,
    "isReconciled": false,
    "summary": "..."
  },
  "analysisResult": {
    "overallStatus": "Success",
    "qualityScore": 99.5,
    "keyFindings": [...],
    "recommendations": [...],
    "criticalIssues": []
  }
}
```

### 2. Start Simple Workflow (Default Settings)
```http
POST /api/workflow/start-simple
```

Starts a workflow with default settings (all records, batch size 100).

### 3. Get Workflow Status
```http
GET /api/workflow/status/{workflowId}
```

**Response**:
```json
{
  "workflowId": "abc-123",
  "currentStage": "Reconciliation",
  "progressPercentage": 70,
  "statusMessage": "Reconciliation completed",
  "lastUpdated": "2024-01-15T10:03:00Z",
  "isCompleted": false,
  "hasErrors": false
}
```

## Testing the Workflow

### 1. Seed Sample Data in MongoDB

Connect to MongoDB and insert sample Party documents:

```javascript
db.Party.insertMany([
  {
    partyId: "PARTY001",
    name: "John Doe",
    type: "Individual",
    email: "john.doe@example.com",
    phone: "+1234567890",
    address: {
      street: "123 Main St",
      city: "New York",
      state: "NY",
      zipCode: "10001",
      country: "USA"
    },
    status: "Active",
    createdDate: new Date("2024-01-01"),
    modifiedDate: new Date("2024-01-15")
  },
  {
    partyId: "PARTY002",
    name: "Acme Corp",
    type: "Organization",
    email: "contact@acme.com",
    phone: "+1987654321",
    address: {
      street: "456 Business Ave",
      city: "San Francisco",
      state: "CA",
      zipCode: "94102",
      country: "USA"
    },
    status: "Active",
    createdDate: new Date("2024-01-05")
  }
])
```

### 2. Run the Workflow

Using cURL:
```bash
curl -X POST https://localhost:5001/api/workflow/start-simple \
  -H "Content-Type: application/json" \
  -k
```

Using PowerShell:
```powershell
Invoke-RestMethod -Uri "https://localhost:5001/api/workflow/start-simple" `
  -Method POST `
  -ContentType "application/json" `
  -SkipCertificateCheck
```

### 3. Verify Migration

Check PostgreSQL:
```sql
SELECT * FROM customers;
```

## Workflow Stages

The workflow progresses through these stages:

1. **NotStarted** (0%) - Workflow initialized
2. **Migration** (10-40%) - Migrating data from MongoDB to PostgreSQL
3. **Reconciliation** (50-70%) - Comparing source and target data
4. **Analysis** (80-95%) - Analyzing reconciliation results
5. **Completed** (100%) - All stages completed successfully
6. **Failed** (100%) - Workflow failed at some stage

## Error Handling

Each agent implements robust error handling:

- **Per-record error tracking**: Failed migrations are logged but don't stop the batch
- **Transaction safety**: Each batch is committed separately
- **Detailed error messages**: All errors include context and stack traces
- **Graceful degradation**: Reconciliation and analysis can still run if migration has partial errors

## Extending the System

### Adding New Agents

1. Create interface in `MigrationWorkflow.Domain/Interfaces`:
```csharp
public interface IMyCustomAgent : IAgent<MyInput, MyOutput>
{
    // Additional methods
}
```

2. Implement in `MigrationWorkflow.Application/Agents`:
```csharp
public class MyCustomAgent : AgentBase<MyInput, MyOutput>, IMyCustomAgent
{
    public override string AgentName => "MyCustomAgent";
    
    public override async Task<MyOutput> ExecuteAsync(MyInput input, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

3. Register in `Program.cs`:
```csharp
builder.Services.AddScoped<IMyCustomAgent, MyCustomAgent>();
```

4. Update `WorkflowOrchestrator` to include the new agent in the workflow.

### Adding New Endpoints

Create new controllers in the `Controllers` folder:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    // Endpoints for viewing historical reports
}
```

## Best Practices for Production

1. **Connection Strings**: Store in Azure Key Vault or environment variables
2. **Logging**: Integrate with Application Insights or Serilog
3. **Status Persistence**: Replace in-memory status tracking with Redis or database
4. **Background Processing**: Use Hangfire or Azure Functions for long-running workflows
5. **Rate Limiting**: Implement rate limiting on API endpoints
6. **Authentication**: Add JWT or OAuth authentication
7. **Database Migrations**: Use EF Core migrations for schema changes
8. **Monitoring**: Add health checks and metrics

## Troubleshooting

### MongoDB Connection Issues
- Verify MongoDB is running: `mongosh --eval "db.version()"`
- Check firewall rules
- Verify connection string format

### PostgreSQL Connection Issues
- Test connection: `psql -h localhost -U postgres -d migration_db`
- Ensure database exists
- Check PostgreSQL service status

### Build Errors
- Ensure .NET 10.0 SDK is installed: `dotnet --version`
- Clean and rebuild: `dotnet clean && dotnet build`
- Restore packages: `dotnet restore`

## License

This project is for educational purposes.

## Support

For questions or issues, please create an issue in the repository.
