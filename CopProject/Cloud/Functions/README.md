# Cloud.Functions

Azure Function that fingerprints crash reports. A Cosmos DB change-feed trigger watches the
`CrashReports` container and `CrashIngestionService` (in `Cloud`) adds each report to a group in
`CrashGroups`.

## Run locally
1. Copy `local.settings.json.example` to `local.settings.json` and fill in the keys (never commit it).
2. Start Azurite (or use a real storage account for `AzureWebJobsStorage`).
3. `func start` in this folder (Azure Functions Core Tools v4).

## Deploy
1. Create a Function App (.NET 10 isolated, Flex Consumption or Consumption).
2. Add app settings `CosmosConnection` (connection string) and `COSMOS_KEY` (account key).
3. `func azure functionapp publish <app-name>`

The trigger creates a `leases` container in `PersistenceDB` the first time it runs.
