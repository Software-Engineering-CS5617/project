## Initial Design for Persistence
- There will be a `Storage Factory` that returns the appropriate storage object based on the requesting module
- Each module will have its own Cosmos DB container
- The `IStorage` interface will define the CRUD operations
- All CRUD operations are done asynchronously
- The caller will be responsible for providing the key and data string when creating or updating an item
- The application modules will interact only with the `IStorage` interface