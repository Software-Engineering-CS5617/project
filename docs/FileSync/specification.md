# FileSync Specification

## Requirements

To develop a file synchronization module for the Cop project with the following features:

1. Allow any module (Incident Management, Whiteboard, Cloud, etc.) to share files from its local folder with other cops using a single button click.
2. Synchronize **large files** (images, videos, documents, whiteboard exports) over the LAN. Small structured data such as incident records is stored in the Cloud and is **out of scope**.
3. Transfer only what is needed: new or changed files are pulled, unchanged files are skipped.
4. Never silently lose a cop's work when two cops edit the same file independently.
5. Expose a small, stable API so other modules integrate without knowing anything about sockets, manifests or conflict logic.
6. Report progress, results and conflicts back to the calling module so that it can display them in its own UI.

<br>

---

## Basic Class Diagram 

```mermaid
graph TD
    FileSync -->|inheritance| ISync
    SyncServer -->|composition| FileSync
    SyncClient -->|composition| FileSync
```

---


## Interface for Other Modules to Use

FileSync is a low-level module that does not directly interact with other modules. It exposes a simple interface (`ISync`) through which modules can access the common shared directory present on every system and save, read, update, and delete files inside it.

All modules are free to create their own subdirectories within this shared directory. For example:

```text
Root/
├── evidences/       # Incident Management
├── WhiteboardLogs/  # Whiteboard
└── ...
```

Modules do not need to implement their own save, read, update, or delete functions. FileSync provides these through the `ISync` interface, and each module simply uses them to manage files within its directory under `Root`.

The FileSync module works independently and synchronizes all files and folders within `Root` across connected systems. This includes files and folders that are created, modified, or deleted.

---

## FileSync API

FileSync provides a simple interface for other modules to access the common shared directory and save, read, update, and delete files.

All file methods take a **relative path** (relative to `Root`). FileSync combines it with `GetDirectory()` internally, so modules never need to build full paths or handle synchronization logic themselves.

```csharp
public interface ISync
{
    // Directory
    string GetDirectory();

    // File operations (relativePath is relative to GetDirectory())
    bool SaveFile(string relativePath, byte[] content);
    byte[] ReadFile(string relativePath);
    bool UpdateFile(string relativePath, byte[] content);
    bool DeleteFile(string relativePath);
    bool FileExists(string relativePath);

}
```

### Method Description

| Method | Description |
|---|---|
| `GetDirectory()` | Returns the path of the shared `Root` directory on the current system. |
| `SaveFile(relativePath, content)` | Creates a new file at `Root/relativePath`. Returns `true` if successful; otherwise, `false`. |
| `ReadFile(relativePath)` | Returns the contents of the file as a `byte[]`. |
| `UpdateFile(relativePath, content)` | Overwrites an existing file. Returns `true` if successful; otherwise, `false`. |
| `DeleteFile(relativePath)` | Deletes a file. Returns `true` if successful; otherwise, `false`. |
| `FileExists(relativePath)` | Returns `true` if the file exists in the shared directory; otherwise, `false`. |
| `OnSyncComplete` | Occurs when file synchronization completes successfully. |
| `OnSyncError` | Occurs when an error occurs during file synchronization and provides an error message. |

### Rules

- `relativePath` must be relative to `Root`. Absolute paths and paths escaping `Root` (e.g., `../`) must be rejected.
- File content is passed as `byte[]`. This keeps the initial interface simple but loads the entire file into memory.
- Save, update, and delete operations are automatically picked up by FileSync's synchronization engine. Modules do not trigger synchronization themselves.
- `ReadFile()` returns the file contents directly. Its error-handling behavior will be defined in the implementation.

### Example Usage by Another Module

```csharp
ISync fileSync = new FileSync();

string rootDirectory = fileSync.GetDirectory();

string evidenceDirectory =
    Path.Combine(rootDirectory, "evidences");
```

Saving, reading, updating, and deleting files:

```csharp
// Save
byte[] imageData = File.ReadAllBytes(localImagePath);
bool saved = fileSync.SaveFile(
    "evidences/incident42/photo.png",
    imageData);

// Read
byte[] fileData = fileSync.ReadFile(
    "evidences/incident42/photo.png");

// Update
bool updated = fileSync.UpdateFile(
    "evidences/incident42/photo.png",
    newImageData);

// Delete
bool deleted = fileSync.DeleteFile(
    "evidences/incident42/photo.png");

// Check existence
bool exists = fileSync.FileExists(
    "evidences/incident42/photo.png");
```

**Note:** Modules use the `ISync` file methods to manage files inside the shared directory. FileSync handles synchronization of the contents of `Root` between systems.

## UI Updates (Event Subscription)

To satisfy Requirement #6 (Reporting progress/results back to the calling module's UI), the FileSync module provides optional event subscriptions. 

Other modules (like Whiteboard or Incident Management) do not command the sync to start. However, if they want to display a "Green Checkmark" or an "Error Popup" on their UI, they can passively tune in to FileSync's events. These events are part of the `ISync` interface:

```csharp
public interface ISync
{
 
    // Along with these methords GetDirectory, SaveFile, ReadFile, DeleteFile, UpdateFile, FileExists 

    // Optional UI Events for Requirement #6
    event Action OnSyncComplete;
    event Action<string> OnSyncError;
}
```

### Example Usage by Another Module

```csharp
// 1. They subscribe their UI functions to our events
fileSync.OnSyncComplete += ShowGreenCheckmark;
fileSync.OnSyncError += ShowErrorPopup;

// 2. The UI Functions (Triggered automatically by FileSync's network engine)
void ShowGreenCheckmark()
{
    // Draw a green checkmark on the screen
}

void ShowErrorPopup(string errorMessage)
{
    // Draw a red box with the exact error string (e.g., "Network disconnected")
}
```

This guarantees **Separation of Concerns**. The sub-modules act like they are writing to a normal hard drive and just listen to events, while FileSync independently handles all network traversal and conflict logic.