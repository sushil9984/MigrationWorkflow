# Multi-Agent Migration Workflow - Project Structure

## Overview

This is a complete multi-agent system built with .NET 10.0 that demonstrates:
- **Agent Pattern**: Three specialized agents working in sequence
- **Clean Architecture**: Separation of concerns across Domain, Application, Infrastructure layers
- **MongoDB to PostgreSQL Migration**: Real-world data migration scenario
- **Automated Reconciliation & Analysis**: Quality assurance built into the workflow

## Complete Project Structure

```
MigrationWorkflow/
├── README.md                           # Comprehensive documentation
├── QUICKSTART.md                       # Quick setup guide
├── MigrationWorkflow.slnx              # Solution file
│
├── MigrationWorkflow.Domain/           # Core business layer
│   ├── Entities/
│   │   ├── Party.cs                    # MongoDB document model
│   │   └── Customer.cs                 # PostgreSQL entity model
│   ├── Interfaces/
│   │   ├── IAgent.cs                   # Base agent interface
│   │   ├── IMigrationAgent.cs          # Migration agent contract
│   │   ├── IReconciliationAgent.cs     # Reconciliation agent contract
│   │   ├── IAnalysisAgent.cs           # Analysis agent contract
│   │   ├── IWorkflowOrchestrator.cs    # Workflow coordinator contract
│   │   ├── IPartyRepository.cs         # Party data access contract
│   │   └── ICustomerRepository.cs      # Customer data access contract
│   └── Models/
│       ├── MigrationRequest.cs         # Migration input model
│       ├── MigrationResult.cs          # Migration output model
│       ├── ReconciliationRequest.cs    # Reconciliation input model
│       ├── ReconciliationReport.cs     # Reconciliation output model
│       ├── AnalysisRequest.cs          # Analysis input model
│       ├── AnalysisResult.cs           # Analysis output model
│       ├── WorkflowRequest.cs          # Workflow input model
│       ├── WorkflowResult.cs           # Workflow output model
│       └── WorkflowStatus.cs           # Workflow status tracking
│
├── MigrationWorkflow.Application/      # Business logic layer
│   ├── Agents/
│   │   ├── AgentBase.cs                # Base class for all agents
│   │   ├── MigrationAgent.cs           # ⭐ Agent 1: Data Migration
│   │   ├── ReconciliationAgent.cs      # ⭐ Agent 2: Data Reconciliation
│   │   └── AnalysisAgent.cs            # ⭐ Agent 3: Analysis & Insights
│   └── Workflow/
│       └── WorkflowOrchestrator.cs     # Coordinates agent execution
│
├── MigrationWorkflow.Infrastructure/   # Data access layer
│   ├── Data/
│   │   ├── MongoDbContext.cs           # MongoDB connection
│   │   └── PostgresDbContext.cs        # PostgreSQL EF Core context
│   └── Repositories/
│       ├── PartyRepository.cs          # Party data access implementation
│       └── CustomerRepository.cs       # Customer data access implementation
│
└── MigrationWorkflow/                  # Web API layer
    ├── Controllers/
    │   └── WorkflowController.cs       # REST API endpoints
    ├── Program.cs                      # Application startup & DI
    ├── appsettings.json                # Production configuration
    ├── appsettings.Development.json    # Development configuration
    └── MigrationWorkflow.http          # API test requests
```

## Key Components

### 🤖 Agent 1: Migration Agent
**File**: `MigrationWorkflow.Application/Agents/MigrationAgent.cs`

**Responsibilities**:
- Read Party documents from MongoDB
- Transform data to Customer entity format
- Write to PostgreSQL database
- Handle batch processing (configurable batch size)
- Track success/failure per record
- Support date range filtering

**Key Methods**:
- `ExecuteAsync()`: Main migration logic
- `GetTotalRecordsToMigrateAsync()`: Pre-migration count
- `MapPartyToCustomer()`: Data transformation
- `UpdateCustomerFromParty()`: Update existing records

### 🔍 Agent 2: Reconciliation Agent
**File**: `MigrationWorkflow.Application/Agents/ReconciliationAgent.cs`

**Responsibilities**:
- Compare record counts (source vs target)
- Validate field-level data accuracy
- Identify discrepancies (Missing, Mismatch, Extra)
- Calculate data accuracy percentage
- Generate comprehensive reconciliation report

**Discrepancy Types**:
- **Missing**: Record exists in source but not in target
- **Mismatch**: Field values differ between source and target
- **Extra**: Record exists in target but not in source

### 📊 Agent 3: Analysis Agent
**File**: `MigrationWorkflow.Application/Agents/AnalysisAgent.cs`

**Responsibilities**:
- Calculate quality score (0-100%)
- Determine overall status (Success/Warning/Failed)
- Generate key findings
- Identify critical issues
- Provide actionable recommendations
- Create detailed analysis report

**Status Thresholds**:
- **Success**: Quality score ≥ 99%
- **Warning**: Quality score ≥ 95%
- **Failed**: Quality score < 95%

### 🎯 Workflow Orchestrator
**File**: `MigrationWorkflow.Application/Workflow/WorkflowOrchestrator.cs`

**Responsibilities**:
- Coordinate agent execution sequence
- Track workflow progress and status
- Handle errors gracefully
- Provide real-time status updates
- Aggregate results from all agents

**Execution Flow**:
```
1. Initialize Workflow (0%)
2. Run Migration Agent (10-40%)
3. Run Reconciliation Agent (50-70%)
4. Run Analysis Agent (80-95%)
5. Complete Workflow (100%)
```

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 10.0 |
| Language | C# 13 |
| Source Database | MongoDB 7.x |
| Target Database | PostgreSQL 16.x |
| ORM | Entity Framework Core 10.x |
| MongoDB Driver | MongoDB.Driver 3.x |
| Web Framework | ASP.NET Core 10.x |
| API Style | REST |
| DI Container | Built-in .NET DI |

## Design Patterns Used

1. **Agent Pattern**: Autonomous agents with specific responsibilities
2. **Repository Pattern**: Abstraction over data access
3. **Dependency Injection**: Loose coupling between components
4. **Clean Architecture**: Separation of concerns
5. **Strategy Pattern**: Different migration strategies can be implemented
6. **Template Method**: AgentBase provides common functionality
7. **Builder Pattern**: Workflow building and configuration

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/workflow/start` | Start workflow with custom parameters |
| POST | `/api/workflow/start-simple` | Start workflow with defaults |
| GET | `/api/workflow/status/{id}` | Get workflow status |

## Configuration Files

### appsettings.json
Contains production database connection strings and general settings.

### appsettings.Development.json
Contains development-specific settings with debug logging enabled.

### MigrationWorkflow.http
Contains pre-configured HTTP requests for testing all endpoints.

## Data Flow

```
┌─────────────┐
│   MongoDB   │
│   (Party)   │
└──────┬──────┘
       │
       │ 1. Read
       ↓
┌─────────────────┐
│ Migration Agent │
│  (Transform)    │
└────────┬────────┘
         │ 2. Write
         ↓
┌─────────────────┐
│   PostgreSQL    │
│   (Customer)    │
└────────┬────────┘
         │
         │ 3. Compare
         ↓
┌──────────────────────┐
│ Reconciliation Agent │
│   (Validate Data)    │
└──────────┬───────────┘
           │ 4. Report
           ↓
┌──────────────────┐
│  Analysis Agent  │
│ (Quality Score)  │
└──────────────────┘
```

## Database Schema

### MongoDB Party Collection
```javascript
{
  _id: ObjectId("..."),
  partyId: String,
  name: String,
  type: String,          // "Individual" or "Organization"
  email: String,
  phone: String,
  address: {
    street: String,
    city: String,
    state: String,
    zipCode: String,
    country: String
  },
  status: String,        // "Active" or "Inactive"
  createdDate: ISODate,
  modifiedDate: ISODate,
  metadata: Object
}
```

### PostgreSQL Customer Table
```sql
CREATE TABLE customers (
    id SERIAL PRIMARY KEY,
    customer_id VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(200) NOT NULL,
    customer_type VARCHAR(50),
    email VARCHAR(100),
    phone VARCHAR(20),
    street VARCHAR(200),
    city VARCHAR(100),
    state VARCHAR(50),
    zip_code VARCHAR(20),
    country VARCHAR(100),
    status VARCHAR(50) DEFAULT 'Active',
    created_date TIMESTAMP NOT NULL,
    modified_date TIMESTAMP,
    source_id VARCHAR(50),        -- Original MongoDB _id
    migrated_at TIMESTAMP         -- Migration timestamp
);

CREATE INDEX idx_customer_id ON customers(customer_id);
CREATE INDEX idx_source_id ON customers(source_id);
CREATE INDEX idx_email ON customers(email);
```

## Extension Points

### Add New Migration Types
Create new entities in Domain layer and implement new agents following the same pattern.

### Add Custom Transformations
Modify `MapPartyToCustomer()` method in `MigrationAgent.cs`.

### Add More Validations
Extend `ReconciliationAgent.cs` with additional `CheckFieldMismatch()` calls.

### Add Business Rules
Implement custom logic in `AnalysisAgent.cs` for domain-specific analysis.

### Add Persistence
Replace in-memory workflow status tracking with database or Redis.

## Testing Strategy

1. **Unit Tests**: Test individual agents with mock repositories
2. **Integration Tests**: Test complete workflow with test databases
3. **Manual Testing**: Use `MigrationWorkflow.http` for API testing
4. **Load Testing**: Test with large datasets (1000+ records)

## Performance Considerations

- **Batch Processing**: Configurable batch size (default: 100)
- **Connection Pooling**: Both MongoDB and PostgreSQL use connection pooling
- **Async/Await**: All operations are asynchronous
- **Memory Efficient**: Processes records in batches to avoid memory issues
- **Error Isolation**: Errors in one record don't affect others

## Security Considerations

- **Connection Strings**: Store in secure configuration (Azure Key Vault, etc.)
- **Input Validation**: Validate all user inputs
- **SQL Injection**: Protected by EF Core parameterization
- **NoSQL Injection**: Protected by MongoDB driver type safety
- **Authentication**: Add JWT/OAuth before production deployment
- **Authorization**: Implement role-based access control

## Monitoring & Logging

- **Structured Logging**: Uses ILogger throughout
- **Agent-Specific Logs**: Each agent logs with `[AgentName]` prefix
- **Workflow Tracking**: Status updates at each workflow stage
- **Error Tracking**: Detailed error messages with context
- **Performance Metrics**: Duration tracking for each phase

## Next Steps for Production

1. **Add Authentication**: Implement JWT or OAuth 2.0
2. **Add Health Checks**: Monitor database connections
3. **Add Metrics**: Export to Prometheus or Application Insights
4. **Add Caching**: Cache frequently accessed data
5. **Add Retry Logic**: Implement Polly for transient failure handling
6. **Add Message Queue**: Use RabbitMQ or Azure Service Bus for long-running jobs
7. **Add API Versioning**: Support multiple API versions
8. **Add Rate Limiting**: Prevent API abuse
9. **Add Docker Support**: Create Dockerfile and docker-compose.yml
10. **Add CI/CD**: Automated build and deployment pipelines

## Developer Resources

- **API Documentation**: See README.md
- **Quick Start**: See QUICKSTART.md
- **HTTP Tests**: See MigrationWorkflow.http
- **Sample Data**: See QUICKSTART.md MongoDB section

## License

This project is for educational purposes and demonstration of multi-agent architecture in .NET.

---

**Built with .NET 10.0 | Clean Architecture | Multi-Agent Pattern**
