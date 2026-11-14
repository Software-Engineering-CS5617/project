// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Cloud.Persistence;

namespace Cloud.Test;

/// <summary>
/// This class contains integration tests for the persistence layer.
/// </summary>
[TestClass]
[Ignore("Skipping Cosmos DB integration tests in CI to avoid interactions with production resources")]
public class TestPersistence
{
    private IStorage _storage = null!;
    private readonly List<string> _keysToCleanUp = [];
    /// <summary>
    /// Current test context. useful for logging
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// This method is called before each test method is executed.
    /// </summary>
    /// <returns>TestContext</returns>
    [TestInitialize]
    public async Task Setup()
    {
        // These are integration tests against the configured Cosmos DB account.
        // we will skip the tests to avoid build failures if the COSMOS_KEY environment variable is not set.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COSMOS_KEY")))
        {
            Assert.Inconclusive("COSMOS_KEY is not set; skipping Cosmos DB integration tests.");
        }

        _storage = await StorageFactory.CreateAsync(ModuleType.Chat);
        TestContext.WriteLine("Starting persistence test against Cosmos DB.");
    }
    /// <summary>
    /// This method is called after each test method is executed.
    /// </summary>
    /// <returns>Task</returns>

    [TestCleanup]
    public async Task CleanUp()
    {
        foreach (string key in _keysToCleanUp)
        {
            await _storage.Delete(key);
        }
    }
    /// <summary>
    /// Tests that creating an item succeeds and rejects duplicate keys.
    /// </summary>
    /// <returns>Task</returns>
    [TestMethod]
    public async Task CreateSucceedsAndRejectsDuplicateKey()
    {
        string key = NewKey();
        byte[] data = Bytes("create");

        Assert.IsTrue(await _storage.Create(key, data));
        Assert.IsFalse(await _storage.Create(key, data), "An existing id must not be created twice.");
    }

    [TestMethod]
    /// <summary>
    /// Tests that reading an item returns its data and reports missing keys.
    /// </summary>
    /// <returns>Task</returns>
    public async Task ReadReturnsDataAndReportsMissingKey()
    {
        string key = NewKey();
        byte[] expected = Bytes("read");
        Assert.IsTrue(await _storage.Create(key, expected));

        (bool found, byte[] actual) = await _storage.Read(key);
        Assert.IsTrue(found);
        CollectionAssert.AreEqual(expected, actual);

        (found, actual) = await _storage.Read(NewKey());
        Assert.IsFalse(found);
        Assert.AreEqual(0, actual.Length);
    }

    [TestMethod]
    ///<summary>
    /// tests reading an item's ETag returns its ETag and reports missing keys.
    ///</summary>
    /// <returns>Task</returns>
    public async Task ReadEtagReturnsEtagAndReportsMissingKey()
    {
        string key = NewKey();
        Assert.IsTrue(await _storage.Create(key, Bytes("etag")));

        (bool found, string etag) = await _storage.ReadEtag(key);
        Assert.IsTrue(found);
        Assert.IsFalse(string.IsNullOrWhiteSpace(etag));

        (found, etag) = await _storage.ReadEtag(NewKey());
        Assert.IsFalse(found);
        Assert.AreEqual(string.Empty, etag);
    }

    [TestMethod]
    /// <summary>
    /// updating an item concurrently using ETags works correctly.
    /// </summary>
    /// returns>Task</returns>
    public async Task UpdateConcurrentlyUsingEtags()
    {
        string key = NewKey();
        byte[] original = Bytes("before");
        byte[] updated = Bytes("after");
        Assert.IsTrue(await _storage.Create(key, original));

        (bool found, string etag) = await _storage.ReadEtag(key);
        Assert.IsTrue(found);
        Assert.IsTrue(await _storage.Update(key, updated, etag));
        Assert.IsFalse(await _storage.Update(key, original, etag), "A stale ETag must be rejected.");
        Assert.IsFalse(await _storage.Update(NewKey(), updated, etag), "Updating a missing item must fail.");

        (bool readSuccess, byte[] actual) = await _storage.Read(key);
        Assert.IsTrue(readSuccess);
        CollectionAssert.AreEqual(updated, actual);
    }

    [TestMethod]
    ///<summary>
    /// Deleting an item removes it and reports missing keys.
    ///</summary>
    public async Task DeleteRemovesItemAndReportsMissingKey()
    {
        string key = NewKey();
        Assert.IsTrue(await _storage.Create(key, Bytes("delete")));

        Assert.IsTrue(await _storage.Delete(key));
        Assert.IsFalse(await _storage.Delete(key));
        (bool found, _) = await _storage.Read(key);
        Assert.IsFalse(found);
    }


    [TestMethod]
    ///<summary>
    /// Creating multiple items creates them and stops on duplicates.
    ///</summary>
    public async Task CreateAllCreatesItemsAndStopsOnDuplicate()
    {
        string firstKey = NewKey();
        string duplicateKey = NewKey();
        Assert.IsTrue(await _storage.Create(duplicateKey, Bytes("existing")));

        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]>()));
        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [firstKey] = Bytes("first"),
            [NewKey()] = Bytes("second")
        }));
        Assert.IsFalse(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [duplicateKey] = Bytes("duplicate")
        }));

        (bool firstFound, byte[] firstData) = await _storage.Read(firstKey);
        Assert.IsTrue(firstFound);
        CollectionAssert.AreEqual(Bytes("first"), firstData);
    }

    [TestMethod]
    ///<summary>
    /// Reading multiple items returns their data and reports missing keys.
    ///</summary>
    public async Task ReadAllReturnsRequestedItemsOrEmptyResultOnMissingKey()
    {
        string firstKey = NewKey();
        string secondKey = NewKey();
        byte[] firstData = Bytes("first");
        byte[] secondData = Bytes("second");
        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [firstKey] = firstData,
            [secondKey] = secondData
        }));

        (bool success, Dictionary<string, byte[]> items) = await _storage.ReadAll([firstKey, secondKey]);
        Assert.IsTrue(success);
        CollectionAssert.AreEqual(firstData, items[firstKey]);
        CollectionAssert.AreEqual(secondData, items[secondKey]);

        (success, items) = await _storage.ReadAll([]);
        Assert.IsTrue(success);
        Assert.AreEqual(0, items.Count);

        (success, items) = await _storage.ReadAll([firstKey, NewKey()]);
        Assert.IsFalse(success);
        Assert.AreEqual(0, items.Count, "A failed bulk read returns an empty dictionary.");
    }

    [TestMethod]
    ///<summary>
    /// Gets all keys in the container and includes a newly created key.
    ///</summary>
    public async Task GetAllKeysIncludesCreatedKey()
    {
        string key = NewKey();
        Assert.IsTrue(await _storage.Create(key, Bytes("key")));

        (bool success, List<string> keys) = await _storage.GetAllKeys();
        Assert.IsTrue(success);
        CollectionAssert.Contains(keys, key);
    }

    [TestMethod]
    ///<summary>
    /// Reading all Etags returns Etags for stored items.
    /// </summary>
    public async Task ReadAllEtagsReturnsEtagsForStoredItems()
    {
        string key = NewKey();
        Assert.IsTrue(await _storage.Create(key, Bytes("etag")));

        (bool success, Dictionary<string, string> etags) = await _storage.ReadAllEtags();
        Assert.IsTrue(success);
        Assert.IsTrue(etags.TryGetValue(key, out string? etag));
        Assert.IsFalse(string.IsNullOrWhiteSpace(etag));
    }

    [TestMethod]
    ///<summary>
    /// Updating multiple items updates them and rejects stale Etags.
    /// </summary>
    public async Task UpdateAllUpdatesAllItemsAndRejectsStaleEtag()
    {
        string firstKey = NewKey();
        string secondKey = NewKey();
        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [firstKey] = Bytes("old first"),
            [secondKey] = Bytes("old second")
        }));

        (bool etagsRead, Dictionary<string, string> etags) = await _storage.ReadAllEtags();
        Assert.IsTrue(etagsRead);
        Dictionary<string, byte[]> updates = new() {
            [firstKey] = Bytes("new first"),
            [secondKey] = Bytes("new second")
        };

        Assert.IsTrue(await _storage.UpdateAll(new Dictionary<string, byte[]>(), etags));
        Assert.IsTrue(await _storage.UpdateAll(updates, etags));
        Assert.IsFalse(await _storage.UpdateAll(updates, etags), "The original ETags are stale now.");

        (bool firstRead, byte[] firstData) = await _storage.Read(firstKey);
        (bool secondRead, byte[] secondData) = await _storage.Read(secondKey);
        Assert.IsTrue(firstRead && secondRead);
        CollectionAssert.AreEqual(Bytes("new first"), firstData);
        CollectionAssert.AreEqual(Bytes("new second"), secondData);
        TestContext.WriteLine($"Test completed for keys: {firstKey}, {secondKey}");
    }

    [TestMethod]
    ///<summary>
    /// Deleting multiple items removes them and reports missing keys.
    ///</summary>
    public async Task DeleteAllDeletesItemsAndReportsMissingKey()
    {
        string firstKey = NewKey();
        string secondKey = NewKey();
        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [firstKey] = Bytes("first"),
            [secondKey] = Bytes("second")
        }));

        Assert.IsTrue(await _storage.DeleteAll([]));
        Assert.IsTrue(await _storage.DeleteAll([firstKey, secondKey]));
        Assert.IsFalse(await _storage.DeleteAll([NewKey()]));

        (bool firstFound, _) = await _storage.Read(firstKey);
        (bool secondFound, _) = await _storage.Read(secondKey);
        Assert.IsFalse(firstFound || secondFound);
    }

    /// <summary>
    /// Tests that ReadEtags returns the ETags for the specified items.
    /// </summary>
    /// <returns></returns>
    public async Task ReadEtagsReturnsEtagsForSpecifiedItems()
    {
        string firstKey = NewKey();
        string secondKey = NewKey();
        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [firstKey] = Bytes("first"),
            [secondKey] = Bytes("second")
        }));

        (bool success, Dictionary<string, string> etags) = await _storage.ReadEtags([firstKey, secondKey]);

        Assert.IsTrue(success);
        Assert.IsTrue(etags.ContainsKey(firstKey));
        Assert.IsTrue(etags.ContainsKey(secondKey));
        Assert.AreEqual(2, etags.Count);
    }
    /// <summary>
    /// Logs the key and ETag of a test item to the test context output.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task LogTestKeyAndETag()
    {
        string key = NewKey();
        byte[] original = Bytes("original");
        byte[] updated = Bytes("updated");
        Assert.IsTrue(await _storage.Create(key, original));
        (bool found, byte[] actual) = await _storage.Read(key);
        Assert.IsTrue(found);
        CollectionAssert.AreEqual(original, actual);
        (bool etagFound, string etag) = await _storage.ReadEtag(key);
        Assert.IsTrue(etagFound);
        Assert.IsTrue(await _storage.Update(key, updated, etag));
        (found, actual) = await _storage.Read(key);
        Assert.IsTrue(found);
        CollectionAssert.AreEqual(updated, actual);
        TestContext.WriteLine($"Test completed for key: {key}");
    }

    [TestMethod]
    /// <summary>
    /// Tests that ReadLatest(n) returns the latest n items from the storage.
    /// </summary>
    /// <returns></returns>
    public async Task ReadLatestReturnsLatestItems()
    {
        string firstKey = NewKey();
        string secondKey = NewKey();
        Assert.IsTrue(await _storage.CreateAll(new Dictionary<string, byte[]> {
            [firstKey] = Bytes("first"),
            [secondKey] = Bytes("second")
        }));
        (bool success, Dictionary<string, byte[]> items) = await _storage.ReadLatest(2);
        Assert.IsTrue(success);
        Assert.AreEqual(2, items.Count);
    }

    /// <summary>
    /// Generates a new unique key for testing and adds it to the cleanup list.
    /// </summary>
    /// <returns>the generated key</returns>
    private string NewKey()
    {
        string key = $"persistence-test-{Guid.NewGuid():N}";
        _keysToCleanUp.Add(key);
        return key;
    }

    /// <summary>
    /// Converts a string to a byte array using UTF-8 encoding
    /// </summary>
    /// <param name="value"></param>
    /// <returns>the generated byte array</returns>
    private static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}
