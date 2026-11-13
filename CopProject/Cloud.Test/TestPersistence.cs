// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Cloud.Persistence;
using Microsoft.Azure.Cosmos;
using Moq;

namespace Cloud.Test;

/// <summary>
/// This class contains unit tests for the persistence layer.
/// Not integrating with an actual Cosmos DB server. Uses Moq to simulate Azure Cosmos DB.
/// </summary>
[TestClass]
public class TestPersistence
{
    /* mock object used to simulate the Cosmos db container */
    private Mock<Container> _mockContainer = null!;
    private IStorage _storage = null!;
    /* 
      this in-memory dictionary acts as the database
          key : item id
          value : PersistenceItem
    */
    private readonly Dictionary<string, PersistenceItem> _items = [];
    /* version counter. 
       Used to generate unique ETags and increment timestamps */
    private int _version;

    /// <summary>
    /// Current test context. useful for logging
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// This method is called before each test method is executed.
    /// Simulate Cosmos DB environment
    /// </summary>
    /// <returns>TestContext</returns>
    [TestInitialize]
    public void Setup()
    {
        _mockContainer = new Mock<Container>();
        _items.Clear();
        _version = 0;
        SetupMockContainer(); /* configure mocked Cosmos DB API methods */

        _storage = new CosmosStorage(
            _mockContainer.Object, /* this mock instance of container has API functions */
            /* iterator factories that simulate Cosmos db LINQ query results */
            () => CreateIterator(_items.Keys.ToList()),
            () => CreateIterator(_items.Values.ToList()),
            count => CreateIterator(_items.Values
                .OrderByDescending(item => long.Parse(item.TimeStamp))
                .Take(count)
                .ToList()));
    }

    /// <summary>
    /// This method is called after each test method is executed.
    /// </summary>
    /// <returns>Task</returns>
    [TestCleanup]
    public void CleanUp()
    {
        /* clears in-memory database */
        _items.Clear();
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
    public async Task ReadAllEtagsReturnsEmptyDictionaryWhenThereAreNoItems()
    {
        (bool success, Dictionary<string, string> etags) = await _storage.ReadAllEtags();

        Assert.IsTrue(success);
        Assert.AreEqual(0, etags.Count);
    }

    [TestMethod]
    ///<summary>
    /// tests the exception branch of ReadAllTags
    ///</summary>
    public async Task ReadAllEtagsReturnsFailureWhenQueryThrows()
    {
        IStorage storage = new CosmosStorage(
            _mockContainer.Object,
            () => CreateIterator(new List<string>()),
            () => throw CosmosError(HttpStatusCode.InternalServerError),
            count => CreateIterator(new List<PersistenceItem>()));

        (bool success, Dictionary<string, string> etags) =
            await storage.ReadAllEtags();

        Assert.IsFalse(success);
        Assert.AreEqual(0, etags.Count);
    }

    [TestMethod]
    public async Task GetAllKeysReturnsFailureWhenQueryThrows()
    {
        IStorage storage = new CosmosStorage(
            _mockContainer.Object,
            () => throw CosmosError(HttpStatusCode.InternalServerError),
            () => CreateIterator(new List<PersistenceItem>()),
            count => CreateIterator(new List<PersistenceItem>()));

        (bool success, List<string> keys) = await storage.GetAllKeys();

        Assert.IsFalse(success);
        Assert.AreEqual(0, keys.Count);
    }

    [TestMethod]
    public async Task ReadAllEtagsReturnsFailureWhenAnItemHasNoEtag()
    {
        IStorage storage = new CosmosStorage(
            _mockContainer.Object,
            () => CreateIterator(new List<string>()),
            () => CreateIterator(new List<PersistenceItem> {
                new() { Id = NewKey(), Etag = null! }
            }),
            count => CreateIterator(new List<PersistenceItem>()));

        (bool success, Dictionary<string, string> etags) = await storage.ReadAllEtags();

        Assert.IsFalse(success);
        Assert.AreEqual(0, etags.Count);
    }

    [TestMethod]
    public async Task ReadEtagsReturnsFailureWhenAKeyIsMissing()
    {
        (bool success, Dictionary<string, string> etags) =
            await _storage.ReadEtags([NewKey()]);

        Assert.IsFalse(success);
        Assert.AreEqual(0, etags.Count);
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
    public async Task UpdateAllReturnsFalseWhenAnItemHasNoEtag()
    {
        Dictionary<string, byte[]> updates = new() {
            [NewKey()] = Bytes("update")
        };

        Assert.IsFalse(await _storage.UpdateAll(updates, new Dictionary<string, string>()));
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
    [TestMethod]
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

    [TestMethod]
    /// <summary>
    /// ReadLatest(n) returns FALSE and an empty dictionary when there are no items in the storage.
    /// </summary>
    /// <returns></returns>
    public async Task ReadLatestRejectsZeroAndNegativeCounts()
    {
        (bool success, Dictionary<string, byte[]> items) = await _storage.ReadLatest(0);
        Assert.IsFalse(success);
        Assert.AreEqual(0, items.Count);

        (success, items) = await _storage.ReadLatest(-1);
        Assert.IsFalse(success);
        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    ///<summary>
    /// hits the exception branch of ReadLatest
    ///</summary>
    public async Task ReadLatestReturnsFailureWhenQueryThrows()
    {
        IStorage storage = new CosmosStorage(
            _mockContainer.Object,
            () => CreateIterator(new List<string>()),
            () => CreateIterator(new List<PersistenceItem>()),
            count => throw CosmosError(HttpStatusCode.InternalServerError));

        (bool success, Dictionary<string, byte[]> items) = await storage.ReadLatest(1);

        Assert.IsFalse(success);
        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    ///<summary>
    /// verifies that the CosmosStorage constructor accepts valid credentials and container names.
    ///</summary>
    public void ConstructorAcceptsCosmosClientAndContainerNames()
    {
        Mock<CosmosClient> client = new();

        CosmosStorage storage = new(client.Object, "TestDatabase", "TestContainer");

        Assert.IsNotNull(storage);
    }

    [TestMethod]
    ///<summary>
    /// verifies CosmosStorage constructor throws exceptions when dependencies are missing
    ///</summary>
    public void ConstructorRejectsMissingDependencies()
    {
        Func<FeedIterator<string>> keys = () => CreateIterator(new List<string>());
        Func<FeedIterator<PersistenceItem>> etags = () => CreateIterator(new List<PersistenceItem>());
        Func<int, FeedIterator<PersistenceItem>> latest =
            _ => CreateIterator(new List<PersistenceItem>());

        Assert.ThrowsExactly<ArgumentNullException>(
            () => new CosmosStorage(null!, keys, etags, latest));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new CosmosStorage(_mockContainer.Object, null!, etags, latest));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new CosmosStorage(_mockContainer.Object, keys, null!, latest));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new CosmosStorage(_mockContainer.Object, keys, etags, null!));
    }


    /// <summary>
    /// Generates a new unique key for testing and adds it to the cleanup list.
    /// </summary>
    /// <returns>the generated key</returns>
    private static string NewKey()
    {
        return $"persistence-test-{Guid.NewGuid():N}";
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

    private void SetupMockContainer()
    {
        /* 
           Setup the mock container to simulate Cosmos DB behavior for unit testing.
           This includes handling Create, Read, Update, and Delete operations with appropriate responses.
        */

        /* Configure mock container's CreateItemAsync method */
        _mockContainer.Setup(container => container.CreateItemAsync(
                It.IsAny<PersistenceItem>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((PersistenceItem item, PartitionKey? _, ItemRequestOptions? _, CancellationToken _) => {
                if (_items.ContainsKey(item.Id))
                {
                    return Task.FromException<ItemResponse<PersistenceItem>>(CosmosError(HttpStatusCode.Conflict));
                }

                item.Etag = NextEtag();
                item.TimeStamp = (++_version).ToString();
                _items.Add(item.Id, item);
                return Task.FromResult(CreateResponse(item, item.Etag));
            });

        /* configure ReadItemAsync method */
        _mockContainer.Setup(container => container.ReadItemAsync<PersistenceItem>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((string key, PartitionKey _, ItemRequestOptions? _, CancellationToken _) => {
                if (!_items.TryGetValue(key, out PersistenceItem? item))
                {
                    return Task.FromException<ItemResponse<PersistenceItem>>(CosmosError(HttpStatusCode.NotFound));
                }

                return Task.FromResult(CreateResponse(item, item.Etag));
            });

        /* configure ReplaceItemAsync method */
        _mockContainer.Setup(container => container.ReplaceItemAsync(
                It.IsAny<PersistenceItem>(), It.IsAny<string>(), It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns((PersistenceItem replacement, string key, PartitionKey? _, ItemRequestOptions? options, CancellationToken _) => {
                if (!_items.TryGetValue(key, out PersistenceItem? current))
                {
                    return Task.FromException<ItemResponse<PersistenceItem>>(CosmosError(HttpStatusCode.NotFound));
                }

                if (options?.IfMatchEtag != current.Etag) /* check etag */
                {
                    return Task.FromException<ItemResponse<PersistenceItem>>(CosmosError(HttpStatusCode.PreconditionFailed));
                }

                replacement.Etag = NextEtag();
                replacement.TimeStamp = (++_version).ToString();
                _items[key] = replacement;
                return Task.FromResult(CreateResponse(replacement, replacement.Etag));
            });


        /* configure DeleteItemAsync method */
        _mockContainer.Setup(container => container.DeleteItemAsync<PersistenceItem>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((string key, PartitionKey _, ItemRequestOptions? _, CancellationToken _) => {
                if (!_items.Remove(key))
                {
                    return Task.FromException<ItemResponse<PersistenceItem>>(CosmosError(HttpStatusCode.NotFound));
                }

                return Task.FromResult(Mock.Of<ItemResponse<PersistenceItem>>());
            });
    }

    private string NextEtag()
    {
        string etag = $"etag-{_version + 1}";
        return etag;
    }

    /* mocked ItemResponse */
    private static ItemResponse<PersistenceItem> CreateResponse(PersistenceItem item, string etag)
    {
        Mock<ItemResponse<PersistenceItem>> response = new();
        response.SetupGet(value => value.Resource).Returns(item);
        response.SetupGet(value => value.ETag).Returns(etag);
        return response.Object;
    }

    private static CosmosException CosmosError(HttpStatusCode statusCode)
    {
        return new CosmosException("Mock Cosmos DB error", statusCode, 0, "mock-activity-id", 0);
    }

    /* mocked iterator */
    private static FeedIterator<T> CreateIterator<T>(IReadOnlyList<T> items)
    {
        Mock<FeedIterator<T>> iterator = new();
        Mock<FeedResponse<T>> response = new();
        response.As<IEnumerable<T>>()
            .Setup(page => page.GetEnumerator())
            .Returns(items.GetEnumerator());

        iterator.SetupSequence(value => value.HasMoreResults)
            .Returns(true)
            .Returns(false);
        iterator.Setup(value => value.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);

        return iterator.Object;
    }
}
