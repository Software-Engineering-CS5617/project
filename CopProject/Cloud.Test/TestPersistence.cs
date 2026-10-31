// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Cloud.Persistence;

namespace Cloud.Test;

[TestClass]
public class TestPersistence
{
    private IStorage _storage = null!;

    [TestInitialize]
    public async Task Setup()
    {

        // CI has no COSMOS_KEY. Skip instead of failing
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COSMOS_KEY")))
        {
            Assert.Inconclusive("COSMOS_KEY is not set, skipping Cosmos integration tests.");
        }
        _storage = await StorageFactory.CreateAsync(ModuleType.Chat);
    }

    /// <summary>
    /// Test 1: Create, Read, Update, Delete.
    /// </summary>
    [TestMethod]
    public async Task TestCRUD()
    {
        string key = "message-011";

        bool created = await _storage.Create(key, Encoding.UTF8.GetBytes("Hello from Chat1"));
        Console.WriteLine($"Item created: {created}");

        (bool Success, byte[] Data) readResult = await _storage.Read(key);
        Console.WriteLine($"Read: {Encoding.UTF8.GetString(readResult.Data)}");

        (bool Success, string Etag) etagResult = await _storage.ReadEtag(key);
        bool updated = await _storage.Update(key, Encoding.UTF8.GetBytes("Updated message"), etagResult.Etag);
        Console.WriteLine($"Item updated: {updated}");

        (bool Success, byte[] Data) readAfterUpdate = await _storage.Read(key);
        Console.WriteLine(
            $"Read bytes after update: {string.Join(", ", readAfterUpdate.Data)}");

        Console.WriteLine(
            $"Read data after update: {Encoding.UTF8.GetString(readAfterUpdate.Data)}");

        bool deleted = await _storage.Delete(key);
        Console.WriteLine($"Item deleted: {deleted}");

    }

    /// <summary>
    /// Test 2: Create, Read, Update, Delete.
    /// </summary>
    [TestMethod]
    public async Task TestUpdate()
    {
        string key2 = "message-019";

        await _storage.Create(key2, Encoding.UTF8.GetBytes("Hello from Chat2"));

        (bool Success, string Etag) etagResult2 = await _storage.ReadEtag(key2);
        await _storage.Update(key2, Encoding.UTF8.GetBytes("Updated message from Chat2"), etagResult2.Etag);


        (bool Success, byte[] Data) readResult2 = await _storage.Read(key2);
        Console.WriteLine($"Read data from key2: {Encoding.UTF8.GetString(readResult2.Data)}");


        await _storage.Delete(key2);
    }

    /// <summary>
    /// Test 3: Simulate concurrent updates.
    /// </summary>
    [TestMethod]
    public async Task TestConcurrentUpdates()
    {
        string key3 = "incident-025";

        await _storage.Create(key3, Encoding.UTF8.GetBytes("Incident reported"));
        Console.WriteLine("Initial incident created.");

        (bool Success, string Etag) etagResultB = await _storage.ReadEtag(key3);
        (bool Success, string Etag) etagResultC = await _storage.ReadEtag(key3);

        byte[] dataB = Encoding.UTF8.GetBytes("Update B");
        byte[] dataC = Encoding.UTF8.GetBytes("Update C");

        Task<bool> updateB = _storage.Update(key3, dataB, etagResultB.Etag);
        Task<bool> updateC = _storage.Update(key3, dataC, etagResultC.Etag);

        bool resultB = await updateB;
        bool resultC = await updateC;

        Console.WriteLine($"User B update: {resultB}");
        Console.WriteLine($"User C update: {resultC}");
        (bool Success, byte[] Data) latestResult = await _storage.Read(key3);
        Console.WriteLine(
            $"Final incident data: {Encoding.UTF8.GetString(latestResult.Data)}");

        await _storage.Delete(key3);
    }
}
