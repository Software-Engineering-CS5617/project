using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence;

// All CRUD operations are asynchronous
// Applications module interacts with the Persistence module through this interface
public interface IStorage
{
    Task<bool> Create(string key, byte[] data);
    Task<(bool Success, byte[] Data)> Read(string key);

    // you have to read the etag before updating, and pass it to the update method
    // This is for concurrency control
    Task<(bool Success, string Etag)> ReadEtag(string key);
    Task<bool> Update(string key, byte[] data, string etag);
    Task<bool> Delete(string key);

    Task<bool> CreateAll(Dictionary<string, byte[]> items);
    Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys);
    Task<(bool Success, List<string>)> GetAllKeys();
    Task<bool> UpdateAll(Dictionary<string, byte[]> items);
    Task<bool> DeleteAll(string[] keys);


}

