using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Incidents.Test.Implementations;

using FileSynchroniser.Models;

public class IncidentFileSynchroniser : IFileOperations
{
    private readonly Dictionary<string, byte[]> _memory = new();

    public string GetDirectory()
    {
        return "Test\\Incidents\\";
    }

    public bool FileExists(string relativePath)
    {
        return _memory.ContainsKey(relativePath);
    }

    public IReadOnlyDictionary<string, bool> SaveFile(IReadOnlyDictionary<string, byte[]> files)
    {
        Dictionary<string, bool> result = new();

        foreach (KeyValuePair<string, byte[]> kvp in files)
        {
            if (_memory.ContainsKey(kvp.Key))
            {
                result[kvp.Key] = false;
            }
            else
            {
                _memory[kvp.Key] = kvp.Value;
                result[kvp.Key] = true;
            }
        }
        return result;
    }

    public IReadOnlyDictionary<string, byte[]?> ReadFile(IEnumerable<string> relativePaths)
    {
        Dictionary<string, byte[]?> result = new();

        foreach (string relativePath in relativePaths)
        {
            if (_memory.ContainsKey(relativePath))
            {
                result[relativePath] = _memory[relativePath];
            }
            else
            {
                result[relativePath] = null;
            }
        }

        return result;
    }

    public IReadOnlyDictionary<string, bool> UpdateFile(IReadOnlyDictionary<string, byte[]> files)
    {
        Dictionary<string, bool> result = new();

        foreach (KeyValuePair<string, byte[]> kvp in files)
        {
            if (_memory.ContainsKey(kvp.Key))
            {
                _memory[kvp.Key] = kvp.Value;
                result[kvp.Key] = true;
            }
            else
            {
                result[kvp.Key] = false;
            }
        }

        return result;
    }

    public IReadOnlyDictionary<string, bool> DeleteFile(IEnumerable<string> relativePaths)
    {
        Dictionary<string, bool> result = new();

        foreach (string relativePath in relativePaths)
        {
            if (_memory.ContainsKey(relativePath))
            {
                _memory.Remove(relativePath);
                result[relativePath] = true;
            }
            else
            {
                result[relativePath] = false;
            }
        }

        return result;
    }
}
