using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests the aggregation of crashes into groups.
/// </summary>
[TestClass]
public class TestCrashGroup
{
    private static CrashReport NewReport(DateTime when, string message = "boom")
    {
        return new CrashReport {
            TimestampUtc = when,
            Exceptions = [
                new CrashExceptionInfo {
                    Type = "System.InvalidOperationException",
                    Message = message,
                    Frames = [new CrashFrame { DeclaringType = "Chat.A", Method = "Run", Assembly = "Chat" }]
                }
            ]
        };
    }

    private static CrashGroup NewGroup(CrashReport report)
    {
        return CrashGroup.FromReport(CrashFingerprinter.Analyze(report), report);
    }

    /// <summary>
    /// A new group copies the signature and holds one crash.
    /// </summary>
    [TestMethod]
    public void FromReportCreatesGroupWithOneCrash()
    {
        DateTime when = new(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);
        CrashReport report = NewReport(when);
        CrashSignature signature = CrashFingerprinter.Analyze(report);

        CrashGroup group = CrashGroup.FromReport(signature, report);

        Assert.AreEqual(signature.Fingerprint, group.Fingerprint);
        Assert.AreEqual(signature.Category, group.Category);
        Assert.AreEqual(signature.Module, group.Module);
        Assert.AreEqual(signature.ExceptionType, group.ExceptionType);
        Assert.AreEqual(signature.TopFrame, group.TopFrame);
        Assert.AreEqual("boom", group.SampleMessage);
        Assert.AreEqual(1, group.Count);
        Assert.AreEqual(when, group.FirstSeenUtc);
        Assert.AreEqual(when, group.LastSeenUtc);
        Assert.HasCount(1, group.RecentReportIds);
    }

    /// <summary>
    /// A very long message is shortened in the group.
    /// </summary>
    [TestMethod]
    public void LongSampleMessageIsTruncated()
    {
        CrashGroup group = NewGroup(NewReport(DateTime.UtcNow, new string('x', 2000)));

        Assert.AreEqual(500, group.SampleMessage.Length);
        Assert.IsTrue(group.SampleMessage.EndsWith("...", StringComparison.Ordinal));
    }

    /// <summary>
    /// A group built from a report without exceptions has an empty sample message.
    /// </summary>
    [TestMethod]
    public void ReportWithoutExceptionsHasEmptySampleMessage()
    {
        CrashReport report = new();

        Assert.AreEqual(string.Empty, NewGroup(report).SampleMessage);
    }

    /// <summary>
    /// Recording another crash increments the count and widens the time range.
    /// </summary>
    [TestMethod]
    public void RecordIncrementsCountAndTracksTimeRange()
    {
        DateTime middle = new(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);
        CrashGroup group = NewGroup(NewReport(middle));

        Assert.IsTrue(group.Record(NewReport(middle.AddHours(2))));
        Assert.IsTrue(group.Record(NewReport(middle.AddHours(-3))));

        Assert.AreEqual(3, group.Count);
        Assert.AreEqual(middle.AddHours(-3), group.FirstSeenUtc);
        Assert.AreEqual(middle.AddHours(2), group.LastSeenUtc);
    }

    /// <summary>
    /// A crash inside the existing time range leaves the range unchanged.
    /// </summary>
    [TestMethod]
    public void RecordInsideRangeKeepsRange()
    {
        DateTime start = new(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);
        CrashGroup group = NewGroup(NewReport(start));
        group.Record(NewReport(start.AddHours(4)));

        group.Record(NewReport(start.AddHours(1)));

        Assert.AreEqual(start, group.FirstSeenUtc);
        Assert.AreEqual(start.AddHours(4), group.LastSeenUtc);
    }

    /// <summary>
    /// Delivering the same report twice counts it once.
    /// </summary>
    [TestMethod]
    public void RecordIgnoresDuplicateReport()
    {
        CrashReport report = NewReport(DateTime.UtcNow);
        CrashGroup group = NewGroup(report);

        Assert.IsFalse(group.Record(report));

        Assert.AreEqual(1, group.Count);
        Assert.HasCount(1, group.RecentReportIds);
    }

    /// <summary>
    /// Only the most recent report IDs are kept, but the count keeps growing.
    /// </summary>
    [TestMethod]
    public void RecentReportIdsAreCapped()
    {
        CrashReport first = NewReport(DateTime.UtcNow);
        CrashGroup group = NewGroup(first);

        for (int i = 0; i < CrashGroup.MaxRecentReports + 5; i++)
        {
            group.Record(NewReport(DateTime.UtcNow));
        }

        Assert.AreEqual(CrashGroup.MaxRecentReports + 6, group.Count);
        Assert.HasCount(CrashGroup.MaxRecentReports, group.RecentReportIds);
        Assert.DoesNotContain(first.Id, group.RecentReportIds);
    }

    /// <summary>
    /// Null arguments are rejected.
    /// </summary>
    [TestMethod]
    public void NullArgumentsThrow()
    {
        CrashReport report = NewReport(DateTime.UtcNow);
        CrashSignature signature = CrashFingerprinter.Analyze(report);

        Assert.ThrowsExactly<ArgumentNullException>(() => CrashGroup.FromReport(null!, report));
        Assert.ThrowsExactly<ArgumentNullException>(() => CrashGroup.FromReport(signature, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => NewGroup(report).Record(null!));
    }
}
