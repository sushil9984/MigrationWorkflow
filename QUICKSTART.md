# Quick Start Guide

## Step 1: Setup MongoDB

### Install MongoDB (if not installed)
- Download from: https://www.mongodb.com/try/download/community
- Or use Docker:
```bash
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

### Seed Sample Data

Connect to MongoDB using `mongosh` or MongoDB Compass and run:

```javascript
// Switch to database
use migration_db

// Insert sample Party documents
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
    name: "Jane Smith",
    type: "Individual",
    email: "jane.smith@example.com",
    phone: "+1234567891",
    address: {
      street: "456 Oak Ave",
      city: "Los Angeles",
      state: "CA",
      zipCode: "90001",
      country: "USA"
    },
    status: "Active",
    createdDate: new Date("2024-01-02")
  },
  {
    partyId: "PARTY003",
    name: "Acme Corp",
    type: "Organization",
    email: "contact@acme.com",
    phone: "+1987654321",
    address: {
      street: "789 Business Blvd",
      city: "San Francisco",
      state: "CA",
      zipCode: "94102",
      country: "USA"
    },
    status: "Active",
    createdDate: new Date("2024-01-05"),
    modifiedDate: new Date("2024-01-20")
  },
  {
    partyId: "PARTY004",
    name: "TechStart Inc",
    type: "Organization",
    email: "info@techstart.com",
    phone: "+1555123456",
    address: {
      street: "321 Innovation Dr",
      city: "Austin",
      state: "TX",
      zipCode: "78701",
      country: "USA"
    },
    status: "Active",
    createdDate: new Date("2024-01-10")
  },
  {
    partyId: "PARTY005",
    name: "Bob Wilson",
    type: "Individual",
    email: "bob.wilson@example.com",
    phone: "+1234567892",
    address: {
      street: "654 Pine St",
      city: "Seattle",
      state: "WA",
      zipCode: "98101",
      country: "USA"
    },
    status: "Inactive",
    createdDate: new Date("2024-01-12"),
    modifiedDate: new Date("2024-01-25")
  }
])

// Verify data
db.Party.countDocuments()
db.Party.find().pretty()
```

## Step 2: Setup PostgreSQL

### Install PostgreSQL (if not installed)
- Download from: https://www.postgresql.org/download/
- Or use Docker:
```bash
docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres --name postgres postgres:16
```

### Create Database

Connect to PostgreSQL and run:

```sql
-- Create database (if it doesn't exist)
CREATE DATABASE migration_db;

-- The Customer table will be auto-created by EF Core on first run
```

## Step 3: Update Configuration

Edit `MigrationWorkflow/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "PostgreSQL": "Host=localhost;Database=migration_db_dev;Username=postgres;Password=postgres"
  },
  "MongoDB": {
    "DatabaseName": "migration_db"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4o-mini"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-openai-api-key",
    "DeploymentName": "your-chat-deployment"
  }
}
```

Notes:
- LLM enhancement is optional. If both `OpenAI` and `AzureOpenAI` are empty, the workflow still runs with deterministic analysis only.
- If both are configured, Azure OpenAI is used first.

## Step 4: Run the Application

```bash
cd MigrationWorkflow
dotnet run
```

The API will be available at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

## Step 5: Test the Workflow

### Option 1: Using the .http file (VS Code with REST Client extension)
Open `MigrationWorkflow.http` and click "Send Request" on any endpoint.

### Option 2: Using cURL
```bash
curl -X POST https://localhost:5001/api/workflow/start-simple \
  -H "Content-Type: application/json" \
  -k
```

### Option 3: Using PowerShell
```powershell
Invoke-RestMethod -Uri "https://localhost:5001/api/workflow/start-simple" `
  -Method POST `
  -ContentType "application/json" `
  -SkipCertificateCheck
```

### Option 4: Using Postman
1. Import the requests from `MigrationWorkflow.http`
2. Send the `start-simple` request

## Step 6: Verify Results

### Check PostgreSQL
```sql
-- Connect to migration_db
\c migration_db

-- View migrated customers
SELECT * FROM customers;

-- Check counts
SELECT COUNT(*) FROM customers;
SELECT customer_type, COUNT(*) FROM customers GROUP BY customer_type;
SELECT status, COUNT(*) FROM customers GROUP BY status;
```

### Check MongoDB
```javascript
// Should still have all original data
db.Party.countDocuments()
```

## Troubleshooting

### MongoDB Connection Error
```
Error: MongoServerSelectionError
```
**Solution**: Ensure MongoDB is running on port 27017

```bash
# Check if MongoDB is running
mongosh --eval "db.version()"

# Or with Docker
docker ps | grep mongo
```

### PostgreSQL Connection Error
```
Error: Npgsql.PostgresException
```
**Solution**: Verify PostgreSQL credentials and database exists

```bash
# Test connection
psql -h localhost -U postgres -d migration_db

# Or with Docker
docker exec -it postgres psql -U postgres -d migration_db
```

### Application Won't Start
```
Error: Unable to start Kestrel
```
**Solution**: Port might be in use

```bash
# Check if port 5001 is available
netstat -an | findstr "5001"  # Windows
lsof -i :5001                 # macOS/Linux

# Change port in launchSettings.json if needed
```

## Next Steps

1. **Explore the API**: Review all endpoints in `MigrationWorkflow.http`
2. **Check Logs**: Watch console output for detailed agent execution logs
3. **Review Analysis**: Examine the analysis results in the workflow response
4. **Customize Mapping**: Modify the `MapPartyToCustomer` method in `MigrationAgent.cs`
5. **Add Validation**: Extend the `ReconciliationAgent` with more field validations
6. **Production Setup**: Follow best practices in README.md for production deployment

## Sample Workflow Response

```json
{
  "workflowId": "abc-123-def-456",
  "success": true,
  "currentStage": "Completed",
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:00:25Z",
  "totalDuration": "00:00:25",
  "migrationResult": {
    "totalRecordsProcessed": 5,
    "successfulMigrations": 5,
    "failedMigrations": 0,
    "duration": "00:00:10"
  },
  "reconciliationReport": {
    "sourceRecordCount": 5,
    "targetRecordCount": 5,
    "mismatchCount": 0,
    "dataAccuracyPercentage": 100.0,
    "isReconciled": true,
    "summary": "Reconciliation completed. Source: 5, Target: 5, Mismatches: 0, Accuracy: 100.00%"
  },
  "analysisResult": {
    "overallStatus": "Success",
    "qualityScore": 100.0,
    "keyFindings": [
      "Total records processed: 5",
      "Data accuracy: 100.00%",
      "Migration is fully reconciled"
    ],
    "recommendations": [
      "Migration completed successfully. Safe to proceed with next steps"
    ],
    "criticalIssues": []
  }
}
```

Congratulations! Your multi-agent workflow system is now running successfully! 🎉
