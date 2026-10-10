using System;

namespace Filesync;

/// <summary>
/// Declares a synchroniser that can synchronise files across shared folder
/// </summary>
public interface ISync
{
    /// <summary>
    /// Does the syncing process for the synchroniser
    /// </summary>
    void Synchronise();


    /// <summary>
    /// Occurs when file synchronisation is completed successfully
    /// </summary>
    event Action OnSyncComplete;

    /// <summary>
    /// Occurs when an error occurs during file synchronisation
    /// </summary>
    /// <param name="errorMessage">Description of the error that occurred</param>
    event Action<string> OnSyncError;

}
