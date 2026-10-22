namespace Persistence;

// All CRUD operations are asynchronous
// Applications module interacts with the Persistence module through this interface

/// <summary>
/// This is the interface for storage operations used by other modules
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Creates a new item in the storage with the specified key and data.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <returns>true if the operation succeeds and false otherwise</returns>
    Task<bool> Create(string key, byte[] data);

    /// <summary>
    /// Reads an item from the storage with the specified key
    /// </summary>
    /// <param name="key"></param>
    /// <returns>Data as byte array and success flag</returns>
    Task<(bool Success, byte[] Data)> Read(string key);

    // you have to read the etag before updating, and pass it to the update method
    // This is for concurrency control

    /// <summary>
    /// Reads the _etag (kind of version stamp) of the Cosmos DB item
    /// </summary>
    /// <param name="key"></param>
    /// <returns>_etag and success flag</returns>
    Task<(bool Success, string Etag)> ReadEtag(string key);

    /// <summary>
    /// Updates a key-value pair 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <param name="etag"></param>
    /// <returns>success flag</returns>
    Task<bool> Update(string key, byte[] data, string etag);

    /// <summary>
    /// Deletes an item specified by the key
    /// </summary>
    /// <param name="key"></param>
    /// <returns>success flag</returns>
    Task<bool> Delete(string key);

    // Following are some bulk CRUD operations
    // They are atomic

    /// <summary>
    /// Creates all the key-value pairs given in the dictionary
    /// </summary>
    /// <param name="items"></param>
    /// <returns>success flag</returns>
    Task<bool> CreateAll(Dictionary<string, byte[]> items);

    /// <summary>
    /// Reads the values of all keys given in the array
    /// </summary>
    /// <param name="keys"></param>
    /// <returns>A boolean flag and the dictionary of all key-value pairs</returns>
    Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys);

    /// <summary>
    /// Reads all the keys in the container
    /// </summary>
    /// <returns>List of all keys from the container of the module</returns>
    Task<(bool Success, List<string>)> GetAllKeys();

    /// <summary>
    /// Reads ETags of all the items in the module's container
    /// </summary>
    /// <returns>A dictionary of Id and Etag</returns>
    Task<(bool Success, Dictionary<string, string>)> ReadAllEtags();
    /// <summary>
    /// Updates all the key-value pairs given in the dictionary
    /// </summary>
    /// <param name="items"></param>
    /// <param name="etags"></param>
    /// <returns>success flag</returns>
    Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags);
    /// <summary>
    /// Deletes values of all keys given in the array
    /// </summary>
    /// <param name="keys"></param>
    /// <returns>success flag</returns>
    Task<bool> DeleteAll(string[] keys);


}

