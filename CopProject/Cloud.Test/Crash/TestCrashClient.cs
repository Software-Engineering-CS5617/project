using System.Runtime.CompilerServices;
using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests report building, the local store, uploading and the crash handler.
/// </summary>
[TestClass]
public class TestCrashClient
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Thrower()
    {
        throw new InvalidOperationException("boom C:\\Users\\alice\\x \u0001");
    }

    private static Exception Thrown()
    {
        try
        {
            Thrower();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException();
    }

    private sealed class ReentrantException : Exception
    {
        private readonly Func<CrashReportOutcome> _report;

        public ReentrantException(Func<CrashReportOutcome> report)
        {
            _report = report;
        }

        public CrashReportOutcome? Inner { get; private set; }

        public override string Message
        {
            get
            {
                Inner = _report();
                return "again";
            }
        }
    }

    /// <summary>
    /// The builder captures type, frames, chain and cleans the text.
    /// </summary>
    [TestMethod]
    public void BuilderCapturesAndCleans()
    {
        Exception inner = Thrown();
        InvalidOperationException outer = new("wrapper", inner);

        CrashReport report = CrashReportBuilder.Build(outer, true, "AppDomain", "u1");

        Assert.HasCount(2, report.Exceptions);
        Assert.AreEqual("System.InvalidOperationException", report.Exceptions[0].Type);
        Assert.HasCount(0, report.Exceptions[0].Frames);
        Assert.AreEqual("Thrower", report.Exceptions[1].Frames[0].Method);
        Assert.AreEqual(typeof(TestCrashClient).Assembly.GetName().Name, report.Exceptions[1].Frames[0].Assembly);
        Assert.AreEqual("boom C:\\Users\\<user>\\x ", report.Exceptions[1].Message);
        Assert.IsTrue(report.IsFatal);
        Assert.AreEqual("u1", report.UserId);
        Assert.AreNotEqual(string.Empty, report.AppVersion);
        CrashXmlSerializer.Serialize(report);
    }

    /// <summary>
    /// Generic declaring types lose their type arguments.
    /// </summary>
    [TestMethod]
    public void BuilderUsesOpenGenericTypeNames()
    {
        Exception ex = new List<int>().Count == 0 ? GenericThrow<int>() : new Exception();

        string type = CrashReportBuilder.Build(ex, false, "t").Exceptions[0].Frames[0].DeclaringType;

        Assert.IsTrue(type.EndsWith("Box`1", StringComparison.Ordinal));
    }

    private static Exception GenericThrow<T>()
    {
        try
        {
            Box<T>.Throw();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException();
    }

    private static class Box<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Throw()
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Null exceptions are rejected.
    /// </summary>
    [TestMethod]
    public void BuilderRejectsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => CrashReportBuilder.Build(null!, false, "x"));
    }

    /// <summary>
    /// The store saves, lists, reads, quarantines, deletes and prunes.
    /// </summary>
    [TestMethod]
    public void StoreSavesListsAndPrunes()
    {
        using TempDir dir = new();
        LocalCrashStore store = new(dir.Path);
        Assert.AreEqual(0, store.PendingCount);

        CrashReport first = Samples.Report(when: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        string path = store.Save(first, CrashXmlSerializer.Serialize(first));

        Assert.IsTrue(path.Contains(first.Id.ToString("D"), StringComparison.Ordinal));
        Assert.AreEqual(first.Id, CrashXmlSerializer.Deserialize<CrashReport>(store.Read(path)).Id);

        store.Quarantine(path);
        Assert.AreEqual(0, store.PendingCount);
        store.Delete(path);

        for (int i = 0; i < LocalCrashStore.MaxPendingReports + 3; i++)
        {
            CrashReport report = Samples.Report(when: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i));
            store.Save(report, CrashXmlSerializer.Serialize(report));
        }

        Assert.AreEqual(LocalCrashStore.MaxPendingReports, store.PendingCount);
        Assert.ThrowsExactly<ArgumentException>(() => new LocalCrashStore(" "));
        Assert.IsTrue(LocalCrashStore.GetDefaultDirectory().EndsWith("pending", StringComparison.Ordinal));
    }

    /// <summary>
    /// Flush uploads and deletes, stops at the first failure and sets bad files aside.
    /// </summary>
    [TestMethod]
    public async Task FlushUploadsStopsOnFailureAndQuarantines()
    {
        using TempDir dir = new();
        LocalCrashStore store = new(dir.Path);

        for (int i = 0; i < 3; i++)
        {
            CrashReport report = Samples.Report(when: new DateTime(2026, 3, 1, 0, 0, i, DateTimeKind.Utc));
            store.Save(report, CrashXmlSerializer.Serialize(report));
        }

        File.WriteAllText(Path.Combine(dir.Path, "00000000T000000000Z_bad.xml"), "<nope");

        FakeUploader uploader = new();
        int calls = 0;
        uploader.Behavior = () => Task.FromResult(++calls < 2);
        CrashPendingUploader pending = new(store, uploader);

        Assert.AreEqual(1, await pending.FlushAsync());
        Assert.AreEqual(2, pending.PendingCount);

        uploader.Behavior = () => Task.FromResult(true);
        Assert.AreEqual(2, await pending.FlushAsync());
        Assert.AreEqual(0, pending.PendingCount);
        Assert.IsTrue(File.Exists(Path.Combine(dir.Path, "00000000T000000000Z_bad.xml.bad")));
    }

    /// <summary>
    /// A second flush while one is running returns immediately.
    /// </summary>
    [TestMethod]
    public async Task ConcurrentFlushReturnsZero()
    {
        using TempDir dir = new();
        LocalCrashStore store = new(dir.Path);
        CrashReport report = Samples.Report();
        store.Save(report, CrashXmlSerializer.Serialize(report));

        TaskCompletionSource<bool> gate = new();
        FakeUploader uploader = new() { Behavior = () => gate.Task };
        CrashPendingUploader pending = new(store, uploader);

        Task<int> first = pending.FlushAsync();
        Assert.AreEqual(0, await pending.FlushAsync());
        gate.SetResult(true);
        Assert.AreEqual(1, await first);
    }

    /// <summary>
    /// A successful upload leaves no local file; a failing one keeps it.
    /// </summary>
    [TestMethod]
    public void HandlerUploadsOrSavesLocally()
    {
        using TempDir dir = new();
        LocalCrashStore store = new(dir.Path);
        FakeUploader uploader = new();
        using CrashHandler handler = new(store, uploader) { UserId = "u9" };

        Assert.AreEqual(CrashReportOutcome.Uploaded, handler.Report(Thrown(), true, "WinUI"));
        Assert.AreEqual(0, store.PendingCount);
        CrashReport sent = CrashXmlSerializer.Deserialize<CrashReport>(uploader.Uploaded[0]);
        Assert.AreEqual("u9", sent.UserId);
        Assert.AreEqual("WinUI", sent.Source);

        uploader.Behavior = () => Task.FromResult(false);
        Assert.AreEqual(CrashReportOutcome.SavedLocally, handler.Report(Thrown()));

        uploader.Behavior = () => throw new IOException("offline");
        Assert.AreEqual(CrashReportOutcome.SavedLocally, handler.Report(Thrown()));
        Assert.AreEqual(2, store.PendingCount);
    }

    /// <summary>
    /// A hanging upload gives up after the timeout.
    /// </summary>
    [TestMethod]
    public void HandlerGivesUpAfterTimeout()
    {
        using TempDir dir = new();
        FakeUploader uploader = new() { Behavior = () => new TaskCompletionSource<bool>().Task };
        using CrashHandler handler = new(new LocalCrashStore(dir.Path), uploader, TimeSpan.FromMilliseconds(200));

        Assert.AreEqual(CrashReportOutcome.SavedLocally, handler.Report(Thrown()));
    }

    /// <summary>
    /// When saving fails the report can still be uploaded, and when both fail it is Failed.
    /// </summary>
    [TestMethod]
    public void HandlerSurvivesStoreFailure()
    {
        using TempDir dir = new();
        Directory.CreateDirectory(dir.Path);
        string blocked = Path.Combine(dir.Path, "file");
        File.WriteAllText(blocked, "x");
        FakeUploader uploader = new();
        using CrashHandler handler = new(new LocalCrashStore(blocked), uploader);

        Assert.AreEqual(CrashReportOutcome.Uploaded, handler.Report(Thrown()));

        uploader.Behavior = () => Task.FromResult(false);
        Assert.AreEqual(CrashReportOutcome.Failed, handler.Report(Thrown()));
    }

    /// <summary>
    /// Nested reports, the session limit and disposal are skipped.
    /// </summary>
    [TestMethod]
    public void HandlerSkipsNestedLimitedAndDisposed()
    {
        using TempDir dir = new();
        CrashHandler handler = new(new LocalCrashStore(dir.Path), new FakeUploader());

        ReentrantException nested = new(() => handler.Report(new InvalidOperationException()));
        Assert.AreEqual(CrashReportOutcome.Uploaded, handler.Report(nested));
        Assert.AreEqual(CrashReportOutcome.Skipped, nested.Inner);

        for (int i = 0; i < CrashHandler.MaxReportsPerSession; i++)
        {
            handler.Report(new InvalidOperationException());
        }

        Assert.AreEqual(CrashReportOutcome.Skipped, handler.Report(new InvalidOperationException()));

        handler.Dispose();
        handler.Dispose();
        Assert.AreEqual(CrashReportOutcome.Skipped, handler.Report(new InvalidOperationException()));
        Assert.ThrowsExactly<ArgumentNullException>(() => handler.Report(null!));
    }

    /// <summary>
    /// The process hooks report fatal and unobserved exceptions.
    /// </summary>
    [TestMethod]
    public void HooksReportExceptions()
    {
        using TempDir dir = new();
        FakeUploader uploader = new();
        using CrashHandler handler = new(new LocalCrashStore(dir.Path), uploader);

        handler.OnUnhandledException(this, new UnhandledExceptionEventArgs(Thrown(), true));
        handler.OnUnhandledException(this, new UnhandledExceptionEventArgs("text", false));
        handler.OnUnobservedTaskException(this, new UnobservedTaskExceptionEventArgs(new AggregateException(Thrown())));

        Assert.HasCount(3, uploader.Uploaded);
        CrashReport fatal = CrashXmlSerializer.Deserialize<CrashReport>(uploader.Uploaded[0]);
        Assert.IsTrue(fatal.IsFatal);
        Assert.AreEqual("AppDomain", fatal.Source);
    }

    /// <summary>
    /// Attach is idempotent, and the Cosmos uploader treats existing reports as uploaded.
    /// </summary>
    [TestMethod]
    public async Task AttachAndCosmosUploader()
    {
        using TempDir dir = new();
        using CrashHandler handler = new(new LocalCrashStore(dir.Path), new FakeUploader());
        handler.Attach();
        handler.Attach();
        handler.FlushPendingInBackground();

        InMemoryStorage storage = new();
        int created = 0;
        CosmosCrashUploader uploader = new(() =>
        {
            created++;
            return Task.FromResult<Cloud.Persistence.IStorage>(storage);
        });

        Guid id = Guid.NewGuid();
        Assert.IsTrue(await uploader.UploadAsync(id, [1], CancellationToken.None));
        Assert.IsTrue(await uploader.UploadAsync(id, [1], CancellationToken.None));
        Assert.AreEqual(1, created);

        CosmosCrashUploader failing = new(() => throw new InvalidOperationException("no key"));
        Assert.IsFalse(await failing.UploadAsync(id, [1], CancellationToken.None));
        Assert.IsFalse(await uploader.UploadAsync(id, [1], new CancellationToken(true)));
    }
}
