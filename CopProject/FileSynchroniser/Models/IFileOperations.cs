// Author : Murali

using System;
using System.Collections.Generic;

namespace FileSynchroniser.Models;

/// <summary>
/// Declares File Operations that synchroniser can use on  files across shared folder.
/// Every operation accepts one or many files; to work on a single file, pass a
/// collection containing one entry.
/// </summary>
public interface IFileOperations
{
    /// <summary>
    /// Returns the path to the root directory that is synchronised across machines
    /// </summary>
    /// <returns>Path to the root directory</returns>
    string GetDirectory();


    /// <summary>
    /// Saves one or more new files in the synchronised root directory.
    /// Existing files are never overwritten.
    /// </summary>
    /// <param name="files">Map of relative path (with respect to the synchronised root directory) to file content</param>
    /// <returns>Map of relative path to success flag, one entry per input file</returns>
    IReadOnlyDictionary<string, bool> SaveFile(IReadOnlyDictionary<string, byte[]> files);

    /// <summary>
    /// Reads one or more files from the synchronised root directory
    /// </summary>
    /// <param name="relativePaths">Relative paths of the files with respect to the synchronised root directory</param>
    /// <returns>Map of relative path to file content, or null if that file could not be read</returns>
    IReadOnlyDictionary<string, byte[]?> ReadFile(IEnumerable<string> relativePaths);

    /// <summary>
    /// Updates one or more existing files in the synchronised root directory
    /// </summary>
    /// <param name="files">Map of relative path to new file content</param>
    /// <returns>Map of relative path to success flag, one entry per input file</returns>
    IReadOnlyDictionary<string, bool> UpdateFile(IReadOnlyDictionary<string, byte[]> files);

    /// <summary>
    /// Deletes one or more files from the synchronised root directory
    /// </summary>
    /// <param name="relativePaths">Relative paths of the files with respect to the synchronised root directory</param>
    /// <returns>Map of relative path to success flag, one entry per input path</returns>
    IReadOnlyDictionary<string, bool> DeleteFile(IEnumerable<string> relativePaths);

    /// <summary>
    /// Check if file exist in the synchronised root directory
    /// </summary>
    /// <param name="relativePath">Relative path of the file with respect to the synchronised root directory</param>
    /// <returns>True if the file exists, false otherwise</returns>
    bool FileExists(string relativePath);

}
