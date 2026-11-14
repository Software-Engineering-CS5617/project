using Cloud.Crash;
using Cloud.Crash.ViewModels;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests ingestion, the report service and the view models.
/// </summary>
[TestClass]
public class TestCrashServer
{
    private static byte[] Xml(CrashReport report)
    {
        return CrashXmlSerializer.Serialize(report);
    }

    private static string Key(CrashReport report)
    {
        return CrashFingerprinter.Analyze(report).Fingerprint;
    }

    /// <summary>
    /// Reports are grouped by fingerprint; duplicates and bad XML are detected.
    /// </summary>
    [TestMethod]
    public async Task IngestionGroupsReports()
    {
        InMemoryStorage storage = new();
        CrashIngestionService service = new(storage, TimeSpan.Zero);
        CrashReport first = Samples.Report();
        CrashReport second = Samples.Report();
        CrashReport other = Samples.Report("System.IO.IOException", "Other");

        Assert.AreEqual(CrashProcessResult.Recorded, await service.ProcessAsync(Xml(first)));
        Assert.AreEqual(CrashProcessResult.Recorded, await service.ProcessAsync(Xml(second)));
        Assert.AreEqual(CrashProcessResult.Duplicate, await service.ProcessAsync(Xml(second)));
        Assert.AreEqual(CrashProcessResult.Recorded, await service.ProcessAsync(Xml(other)));
        Assert.AreEqual(CrashProcessResult.Invalid, await service.ProcessAsync([1, 2]));

        CrashGroup group = CrashXmlSerializer.Deserialize<CrashGroup>((await storage.Read(Key(first))).Data);
        Assert.AreEqual(2, group.Count);
        Assert.HasCount(2, (await storage.GetAllKeys()).Item2);
    }

    /// <summary>
    /// Stale ETags are retried, a lost create race falls back to update, and persistent conflicts are reported.
    /// </summary>
    [TestMethod]
    public async Task IngestionHandlesConflicts()
    {
        CrashReport report = Samples.Report();
        InMemoryStorage storage = new();
        CrashIngestionService service = new(storage, TimeSpan.Zero);
        await service.ProcessAsync(Xml(Samples.Report()));

        storage.FailNextUpdates = 2;
        Assert.AreEqual(CrashProcessResult.Recorded, await service.ProcessAsync(Xml(report)));

        storage.FailNextUpdates = CrashIngestionService.MaxAttempts;
        Assert.AreEqual(CrashProcessResult.Conflict, await service.ProcessAsync(Xml(Samples.Report())));

        InMemoryStorage racing = new();
        CrashReport winner = Samples.Report();
        racing.BeforeCreate = key => {
            racing.BeforeCreate = null;
            racing.Put(key, CrashXmlSerializer.Serialize(Samples.Group(winner)));
        };
        Assert.AreEqual(CrashProcessResult.Recorded, await new CrashIngestionService(racing, TimeSpan.Zero).ProcessAsync(Xml(Samples.Report())));
        Assert.AreEqual(2, CrashXmlSerializer.Deserialize<CrashGroup>((await racing.Read(Key(winner))).Data).Count);
    }

    /// <summary>
    /// A corrupt group document is replaced.
    /// </summary>
    [TestMethod]
    public async Task IngestionReplacesCorruptGroup()
    {
        InMemoryStorage storage = new();
        CrashReport report = Samples.Report();
        await storage.Put(Key(report), [9, 9]);

        Assert.AreEqual(CrashProcessResult.Recorded, await new CrashIngestionService(storage, TimeSpan.Zero).ProcessAsync(Xml(report)));
        Assert.AreEqual(1, CrashXmlSerializer.Deserialize<CrashGroup>((await storage.Read(Key(report))).Data).Count);
    }

    /// <summary>
    /// The service lists groups newest first, skips bad data and loads reports.
    /// </summary>
    [TestMethod]
    public async Task ServiceReadsGroupsAndReports()
    {
        InMemoryStorage groups = new();
        InMemoryStorage reports = new();
        CrashReport older = Samples.Report("System.IO.IOException", when: DateTime.UtcNow.AddDays(-2));
        CrashReport newer = Samples.Report(when: DateTime.UtcNow);
        await groups.Put("a", CrashXmlSerializer.Serialize(Samples.Group(older)));
        await groups.Put("b", CrashXmlSerializer.Serialize(Samples.Group(newer)));
        await groups.Put("bad", [1]);
        await reports.Put(newer.Id.ToString("D"), Xml(newer));
        CrashReportService service = new(groups, reports);

        IReadOnlyList<CrashGroup> list = await service.GetGroupsAsync();
        Assert.HasCount(2, list);
        Assert.AreEqual("System.InvalidOperationException", list[0].ExceptionType);

        CrashGroup withMissing = Samples.Group(newer);
        withMissing.RecentReportIds.Add(Guid.NewGuid());
        Assert.HasCount(1, await service.GetReportsAsync(withMissing));

        groups.FailGetAllKeys = true;
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.GetGroupsAsync());
        Assert.AreEqual(0, service.GetPendingCount());
        Assert.HasCount(0, service.GetPendingReports());
        Assert.AreEqual(0, await service.RetryPendingAsync());
    }

    /// <summary>
    /// The service reports and uploads pending reports.
    /// </summary>
    [TestMethod]
    public async Task ServiceUsesPendingUploader()
    {
        using TempDir dir = new();
        LocalCrashStore store = new(dir.Path);
        CrashReport report = Samples.Report();
        store.Save(report, Xml(report));
        CrashReportService service = new(new InMemoryStorage(), new InMemoryStorage(), new CrashPendingUploader(store, new FakeUploader()));

        Assert.AreEqual(1, service.GetPendingCount());
        Assert.HasCount(1, service.GetPendingReports());
        Assert.AreEqual(1, await service.RetryPendingAsync());
        Assert.AreEqual(0, service.GetPendingCount());
    }

    /// <summary>
    /// The dashboard loads, filters, keeps its module choice and shows errors.
    /// </summary>
    [TestMethod]
    public async Task DashboardLoadsAndFilters()
    {
        FakeCrashReportService fake = new() {
            Pending = 2,
            PendingReports = [Samples.Report()],
            Groups = [Samples.Group(Samples.Report()), Samples.Group(Samples.Report("System.IO.IOException", "Read", "Networking"))]
        };
        CrashDashboardViewModel vm = new(fake);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await vm.RefreshCommand.ExecuteAsync();
        Assert.HasCount(2, vm.Groups);
        Assert.IsTrue(vm.HasPending);
        Assert.IsTrue(vm.PendingMessage.StartsWith("2", StringComparison.Ordinal));
        Assert.HasCount(3, vm.ModuleOptions);
        Assert.HasCount(1, vm.PendingReports);
        Assert.IsTrue(vm.HasPendingReports);
        Assert.AreEqual("InvalidOperationException", vm.PendingReports[0].ExceptionTypeText);
        Assert.AreEqual("Saved on this device", vm.PendingReports[0].StatusText);
        Assert.IsFalse(string.IsNullOrEmpty(vm.PendingReports[0].TopFrame));

        vm.SelectedModule = "Networking";
        Assert.HasCount(1, vm.Groups);
        vm.SelectedModule = null!;
        Assert.AreEqual("Networking", vm.SelectedModule);
        await vm.LoadAsync();
        Assert.AreEqual("Networking", vm.SelectedModule);

        vm.ClearFilters();
        vm.SelectedCategory = "FileSystem";
        Assert.AreEqual("Read", vm.Groups[0].TopFrame.Split('.')[^1]);
        vm.SelectedCategory = "NullReference";
        Assert.IsTrue(vm.IsEmpty);
        vm.ClearFiltersCommand.Execute(null);
        vm.SearchText = "invalidoperation";
        Assert.HasCount(1, vm.Groups);
        Assert.AreEqual("InvalidOperationException", vm.Groups[0].ShortExceptionType);
        Assert.IsTrue(changed.Contains("Groups") || changed.Contains("IsEmpty"));

        fake.Groups = [];
        fake.Fail = true;
        await vm.LoadAsync();
        Assert.IsTrue(vm.HasError);
    }

    /// <summary>
    /// The test-crash command reports through the handler and shows the report as local or uploaded.
    /// </summary>
    [TestMethod]
    public async Task DashboardReportsTestCrash()
    {
        Assert.AreEqual("The crash handler is not installed.", await RunWithoutHandler());

        using TempDir dir = new();
        LocalCrashStore store = new(dir.Path);
        FakeUploader uploader = new() { Behavior = () => Task.FromResult(false) };
        using CrashHandler handler = new(store, uploader);
        CrashReportService service = new(new InMemoryStorage(), new InMemoryStorage(), handler.PendingUploader);
        CrashDashboardViewModel vm = new(service, handler);

        await vm.ReportTestCrashCommand.ExecuteAsync();
        Assert.HasCount(1, vm.PendingReports);
        Assert.IsTrue(vm.StatusMessage.Contains("saved on this device", StringComparison.Ordinal));
        Assert.IsTrue(vm.HasStatus);

        uploader.Behavior = () => Task.FromResult(true);
        await vm.ReportTestCrashAsync();
        Assert.HasCount(1, vm.PendingReports);
        Assert.IsTrue(vm.StatusMessage.Contains("uploaded to the cloud", StringComparison.Ordinal));
    }

    private static async Task<string> RunWithoutHandler()
    {
        CrashDashboardViewModel vm = new(new FakeCrashReportService());
        await vm.ReportTestCrashAsync();
        return vm.StatusMessage;
    }

    /// <summary>
    /// Retrying pending uploads updates the status and handles failure.
    /// </summary>
    [TestMethod]
    public async Task DashboardRetriesPending()
    {
        FakeCrashReportService fake = new() { Pending = 1, RetryResult = 1 };
        CrashDashboardViewModel vm = new(fake);

        await vm.RetryPendingCommand.ExecuteAsync();
        Assert.AreEqual("Uploaded 1 report(s).", vm.StatusMessage);

        fake.RetryResult = 0;
        await vm.RetryPendingAsync();
        Assert.AreEqual("Nothing could be uploaded.", vm.StatusMessage);

        fake.Fail = true;
        await vm.RetryPendingAsync();
        Assert.IsTrue(vm.HasError);
    }

    /// <summary>
    /// The details view model loads reports, selects the newest and shows frames.
    /// </summary>
    [TestMethod]
    public async Task DetailsLoadsReports()
    {
        CrashReport report = Samples.Report();
        report.FullText = "full text";
        report.Exceptions[0].Frames[0].FileName = "A.cs";
        report.Exceptions[0].Frames[0].Line = 7;
        FakeCrashReportService fake = new() { Reports = [report] };
        CrashDetailsViewModel vm = new(fake, Samples.Group(report));

        await vm.RefreshCommand.ExecuteAsync();

        Assert.HasCount(1, vm.Reports);
        Assert.AreEqual("full text", vm.SelectedReportText);
        Assert.IsTrue(vm.SelectedFramesText.Contains("A.cs:7", StringComparison.Ordinal));
        Assert.AreEqual(vm.Group.Fingerprint, vm.Fingerprint);
        Assert.AreEqual("1", vm.CountText);
        Assert.IsTrue(vm.Reports[0].Summary.Contains("unknown user", StringComparison.Ordinal));
        Assert.IsFalse(string.IsNullOrEmpty(vm.FirstSeenText + vm.LastSeenText + vm.Title + vm.TopFrame + vm.CategoryText + vm.Module + vm.SampleMessage));
        Assert.AreEqual("Manual", new CrashReportItem(new CrashReport { Source = "Manual" }).SourceText);

        fake.Fail = true;
        await vm.LoadAsync();
        Assert.IsTrue(vm.HasError);
    }

    /// <summary>
    /// Commands respect CanExecute and do not run twice at once.
    /// </summary>
    [TestMethod]
    public async Task CommandsRespectCanExecute()
    {
        int runs = 0;
        RelayCommand relay = new(() => runs++, () => runs < 1);
        int raised = 0;
        relay.CanExecuteChanged += (_, _) => raised++;

        Assert.IsTrue(relay.CanExecute(null));
        relay.Execute(null);
        relay.RaiseCanExecuteChanged();
        Assert.IsFalse(relay.CanExecute(null));
        Assert.AreEqual(1, raised);

        TaskCompletionSource gate = new();
        AsyncRelayCommand command = new(() => gate.Task);
        Task running = command.ExecuteAsync();
        Assert.IsFalse(command.CanExecute(null));
        await command.ExecuteAsync();
        gate.SetResult();
        await running;
        Assert.IsTrue(command.CanExecute(null));
        command.Execute(null);
    }
}
