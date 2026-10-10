using System;

namespace Filesync;

/// <summary>
/// Declares a synchroniser that can synchronise files across shared folder
/// </summary>
public interface ISync
{
    /// <summary>
    /// Returns the path to the root directory that is synchronised across machines
    /// </summary>
    /// <returns>Path to the root directory</returns>
    string GetDirectory();


    /// <summary>
    /// Saves a new file in the synchronised root directory
    /// </summary>
    /// <param name="relativePath">Relative path of the file with respect to the synchronised root directory</param>
    /// <param name="content">Byte array containing the file content to be saved</param>
    /// <returns>True if the file is saved successfully, false otherwise</returns>
    bool SaveFile(string relativePath, byte[] content);

    /// <summary>
    /// Reads the file in the synchronised root directory
    /// </summary>
    /// <param name="relativePath">Relative path of the file with respect to the synchronised root directory</param>
    /// <returns>True if the file is saved successfully, false otherwise</returns>
    byte[] ReadFile(string relativePath);

    /// <summary>
    /// Updates the file in the synchronised root directory
    /// </summary>
    /// <param name="relativePath">Relative path of the file with respect to the synchronised root directory</param>
    /// <returns>True if the file is update successfully, false otherwise</returns>
    bool UpdateFile(string relativePath, byte[] content);

    /// <summary>
    /// Deletes the file in the synchronised root directory
    /// </summary>
    /// <param name="relativePath">Relative path of the file with respect to the synchronised root directory</param>
    /// <returns>True if the file is deleted successfully, false otherwise</returns>
    bool DeleteFile(string relativePath);

    /// <summary>
    /// Check if file exist in the synchronised root directory
    /// </summary>
    /// <param name="relativePath">Relative path of the file with respect to the synchronised root directory</param>
    /// <returns>True if the file is exist successfully, false otherwise</returns>
    bool FileExists(string relativePath);


    /// <summary>
    /// Occurs when file synchronisation is completed successfully.
    /// </summary>
    event Action OnSyncComplete;

    /// <summary>
    /// Occurs when an error occurs during file synchronisation.
    /// </summary>
    /// <param name="errorMessage">Description of the error that occurred.</param>
    event Action<string> OnSyncError;

}
