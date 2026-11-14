# Crash Handler: Architecture

Captures unhandled exceptions, uploads them to the cloud (or keeps them on disk when offline),
groups identical crashes by fingerprint on Azure, and shows the groups in a dashboard.
Everything lives under `Cloud/`. All data we create is **XML**.

## 1. Flow

```
 Application (UX / any module)
   |  unhandled exception
   v
 CrashHandler ---------------------------------------------------------------+
   1. CrashReportBuilder   exception -> CrashReport (cleaned, scrubbed)      |
   2. CrashXmlSerializer   CrashReport -> XML bytes                          |
   3. LocalCrashStore      save XML to %LocalAppData%\CopProject\Crashes\pending
   4. CosmosCrashUploader  upload to Cosmos "CrashReports" (5 s timeout)     |
        success -> delete local file                                         |
        failure -> file stays; retried at next startup (CrashPendingUploader)|
                                                                             |
 Cosmos "CrashReports" container  (key = report ID, value = report XML)  <---+
   |  change feed
   v
 Azure Function  CrashFingerprintFunction
   -> reads the report XML -> CrashIngestionService
        CrashFingerprinter  (StackNormalizer + CrashCategorizer + ModuleCatalog)
        add to / create group, using the ETag to be safe with parallel instances
   v
 Cosmos "CrashGroups" container  (key = fingerprint, value = group XML)
   ^
   |  CrashReportService (read only)
 Dashboard UI (Cloud/UI)  <--  view models in Cloud/Crash/ViewModels
```

## 2. Key decisions

| Decision | Reason |
|---|---|
| Save locally **before** uploading | The process may die before the upload ends. |
| Upload with a 5 s timeout on a thread-pool thread | A crashing app must not hang, and waiting cannot deadlock the UI thread. |
| Fingerprint on Azure via a Cosmos change-feed trigger | The client only needs `IStorage.Create`. If the function is down, reports wait in Cosmos and are processed later. |
| Two containers (`CrashReports`, `CrashGroups`) | The function's own writes do not re-trigger it. |
| Group updates use `ReadEtag` then `Update` with retry | Several function instances can hit one group without losing counts. |
| Re-counting the same report is ignored | The change feed delivers at least once. |
| Raw reports are kept | We can re-fingerprint later if the algorithm changes (`FingerprintVersion`). |
| View models live in `Cloud` (no WinUI types) | They are unit-tested in `Cloud.Test` and count towards the coverage gate. |

## 3. Fingerprint

`SHA-256( "v1" | root-cause exception type | top 5 normalized application frames )`, first 8 bytes as 16 hex characters.

- **Root cause** = innermost exception that has a stack (skips `AggregateException` wrappers).
- **Application frames** = frames from our own assemblies (`ModuleCatalog`); framework frames are used only if there are none.
- **Normalization** removes line numbers, lambda and async-state-machine names, generic arguments; repeated (recursive) frames collapse.
- **Category** comes from the exception type name (`NullReference`, `Network`, `FileSystem`, ...).

## 4. Data formats and keys

| Where | Key | Content |
|---|---|---|
| Local pending folder | `<timestamp>_<reportId>.xml` | `CrashReport` XML (at most 100 kept; unreadable files become `.bad`) |
| Cosmos `CrashReports` | report ID | `CrashReport` XML |
| Cosmos `CrashGroups` | fingerprint | `CrashGroup` XML (count, first/last seen, last 10 report IDs) |

Both XML roots carry a `schemaVersion` attribute. DTDs are rejected when reading.

## 5. Files

### `Cloud/Crash/` (core library)

| File | Purpose |
|---|---|
| `CrashReport.cs` | One captured crash: ID, time, fatal flag, source, app/OS info, user, exception chain, full text. XML root. |
| `CrashExceptionInfo.cs` | One exception in the chain: type, message, frames. |
| `CrashFrame.cs` | One stack frame: type, method, assembly, file, line. |
| `CrashGroup.cs` | All crashes with one fingerprint; `FromReport` creates, `Record` counts (ignores duplicates). |
| `CrashCategory.cs` | Enum of failure kinds shown in the UI. |
| `CrashSignature.cs` | Result of analysis: fingerprint, type, top frame, module, category. |
| `CrashXmlSerializer.cs` | Serializes/deserializes reports and groups as XML; safe against DTDs; throws `InvalidDataException` on bad input. |
| `CrashText.cs` | Strips characters illegal in XML, replaces user names in paths with `<user>`, truncates text. |
| `ModuleCatalog.cs` | Maps assembly names (Chat, Networking, ...) to module names. |
| `StackNormalizer.cs` | Turns a frame into a stable `Namespace.Type.Method` name. |
| `CrashCategorizer.cs` | Exception type name to `CrashCategory`. |
| `CrashFingerprinter.cs` | Picks root cause and frames, builds the fingerprint, returns a `CrashSignature`. |
| `CrashReportBuilder.cs` | Live `Exception` to `CrashReport` (chain, frames, cleaned text). |
| `CrashHandler.cs` | Entry point. `Install()`, hooks (AppDomain, TaskScheduler), `Report(...)`; never throws; limit of 20 reports per run. |
| `CrashReportOutcome.cs` | Result of `Report`: Uploaded, SavedLocally, Failed, Skipped. |
| `LocalCrashStore.cs` | Atomic save, list, read, delete, quarantine and pruning of pending XML files. |
| `ICrashUploader.cs` | Upload abstraction (lets tests avoid Azure). |
| `CosmosCrashUploader.cs` | Uploads to the `CrashReports` container through `IStorage`; an existing report counts as success. |
| `CrashPendingUploader.cs` | Uploads leftover local reports oldest first; stops at the first failure. |
| `CrashIngestionService.cs` | What runs on Azure: parse, fingerprint, create or update the group with ETag retry. |
| `CrashProcessResult.cs` | Result of ingestion: Recorded, Duplicate, Invalid, Conflict. |
| `ICrashReportService.cs` | Read API for the UI. |
| `CrashReportService.cs` | Reads groups and reports from Cosmos; pending count and retry. |

### `Cloud/Crash/ViewModels/` (UI logic, no WinUI dependency)

| File | Purpose |
|---|---|
| `ViewModelBase.cs` | `INotifyPropertyChanged` helper. |
| `RelayCommand.cs`, `AsyncRelayCommand.cs` | Commands; the async one is disabled while running. |
| `CrashGroupItem.cs`, `CrashReportItem.cs` | Display wrappers for list rows. |
| `CrashDashboardViewModel.cs` | Group list with search, category and module filters, pending banner, retry, errors. |
| `CrashDetailsViewModel.cs` | One group, its reports, selected report text and frames. |

### `Cloud/Persistence/` (two small changes to existing files)

| File | Change |
|---|---|
| `ModuleTypes.cs` | Added `CrashReports` and `CrashGroups`. |
| `StorageFactory.cs` | Maps them to containers `"CrashReports"` and `"CrashGroups"`. |

### `Cloud/Functions/` (Azure Function app)

| File | Purpose |
|---|---|
| `Cloud.Functions.csproj` | .NET 10 isolated Functions project; references `Cloud`. |
| `Program.cs` | Starts the Functions host. |
| `CrashFingerprintFunction.cs` | Cosmos change-feed trigger; reads each new report through `IStorage` and passes it to `CrashIngestionService`; fails the batch on conflicts so it is retried. |
| `CrashReportDocument.cs` | Holds only the document ID from the trigger. |
| `host.json` | Logging and retry policy (JSON is required by Azure). |
| `local.settings.json.example` | Template for local settings; copy to `local.settings.json`, never commit. |
| `README.md` | Local run and deployment steps. |

### `Cloud/UI/` (WinUI library)

| File | Purpose |
|---|---|
| `Cloud.UI.csproj` | WinUI project; references `Cloud`. |
| `Views/CrashDashboardPage.xaml(.cs)` | Group table, filters, pending-upload banner, error bar. |
| `Views/CrashDetailsPage.xaml(.cs)` | Group summary, report list, frames and full stack. |
| `Views/CrashPageContext.cs` | Navigation parameters for both pages. |

### `Cloud/UIharness/` (test app)

| File | Purpose |
|---|---|
| `App.xaml(.cs)`, `MainWindow.xaml(.cs)` | Hosts the dashboard on its own; uses real data when `COSMOS_KEY` is set. |
| `SampleCrashReportService.cs` | In-memory fake crashes so the UI runs without Azure. |

### `Cloud.Test/` (tests, 98 passing)

| File | Covers |
|---|---|
| `Crash/TestStackNormalizer.cs`, `TestCrashCategorizer.cs`, `TestModuleCatalog.cs`, `TestCrashText.cs` | Normalization, categories, modules, text cleaning. |
| `Crash/TestCrashXmlSerializer.cs`, `TestCrashGroup.cs`, `TestCrashFingerprinter.cs` | XML round trips, grouping, fingerprint stability. |
| `Crash/TestCrashClient.cs` | Builder, local store, pending uploads, handler (timeout, limits, hooks), Cosmos uploader. |
| `Crash/TestCrashServer.cs` | Ingestion conflicts, report service, view models, commands. |
| `Crash/CrashTestSupport.cs` | Fakes: in-memory storage with ETags, fake uploader and report service, sample data. |
| `TestStorageFactory.cs` | Updated mapping test for the two new containers. |

## 6. Outside `Cloud/` (see `CRASH-INTEGRATION.md`)

- `UX/UX.csproj`: retarget to net10 and reference `Cloud` and `Cloud/UI`.
- `UX/App.xaml.cs`: call `CrashHandler.Install()` and forward `Application.UnhandledException` to `CrashHandler.Current.Report`.
- `Cloud/Cloud.csproj`: exclude the `Functions`, `UI` and `UIharness` sub-folders from compilation.
- `CopProject.slnx` and `.gitignore` (`local.settings.json`).
- Azure: a Function App with the settings `CosmosConnection` and `COSMOS_KEY`.

## 7. Limits

- At most 20 reports per run, at most 100 pending files, groups remember the last 10 report IDs.
- Two overloads of the same method produce the same frame name.
- The Function, UI and harness projects were written but not compiled in the development sandbox.
