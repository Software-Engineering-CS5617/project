using System.Text;
using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests XML round trips of crash reports and groups.
/// </summary>
[TestClass]
public class TestCrashXmlSerializer
{
    private static CrashReport NewReport()
    {
        return new CrashReport {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TimestampUtc = new DateTime(2026, 11, 6, 10, 30, 0, DateTimeKind.Utc),
            IsFatal = true,
            Source = "AppDomain",
            AppVersion = "1.2.3",
            OsDescription = "Windows 11",
            UserId = "user-1",
            FullText = "System.InvalidOperationException: boom\r\n   at Chat.ChatServer.Run()",
            Exceptions = [
                new CrashExceptionInfo {
                    Type = "System.AggregateException",
                    Message = "One or more errors occurred.",
                    Frames = []
                },
                new CrashExceptionInfo {
                    Type = "System.InvalidOperationException",
                    Message = "boom <&> \"quoted\"",
                    Frames = [
                        new CrashFrame { DeclaringType = "Chat.ChatServer", Method = "Run", Assembly = "Chat", FileName = "ChatServer.cs", Line = 42 },
                        new CrashFrame { DeclaringType = "Chat.Program", Method = "Main", Assembly = "Chat" }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// Every field of a report survives a round trip.
    /// </summary>
    [TestMethod]
    public void ReportRoundTripsAllFields()
    {
        CrashReport original = NewReport();

        CrashReport copy = CrashXmlSerializer.Deserialize<CrashReport>(CrashXmlSerializer.Serialize(original));

        Assert.AreEqual(CrashReport.CurrentSchemaVersion, copy.SchemaVersion);
        Assert.AreEqual(original.Id, copy.Id);
        Assert.AreEqual(original.TimestampUtc, copy.TimestampUtc.ToUniversalTime());
        Assert.IsTrue(copy.IsFatal);
        Assert.AreEqual("AppDomain", copy.Source);
        Assert.AreEqual("1.2.3", copy.AppVersion);
        Assert.AreEqual("Windows 11", copy.OsDescription);
        Assert.AreEqual("user-1", copy.UserId);
        Assert.AreEqual(original.FullText, copy.FullText);
        Assert.HasCount(2, copy.Exceptions);
        Assert.AreEqual("boom <&> \"quoted\"", copy.Exceptions[1].Message);
        Assert.HasCount(2, copy.Exceptions[1].Frames);
        Assert.AreEqual("ChatServer.cs", copy.Exceptions[1].Frames[0].FileName);
        Assert.AreEqual(42, copy.Exceptions[1].Frames[0].Line);
        Assert.IsNull(copy.Exceptions[1].Frames[1].FileName);
    }

    /// <summary>
    /// A missing user ID stays null.
    /// </summary>
    [TestMethod]
    public void NullUserIdStaysNull()
    {
        CrashReport original = NewReport();
        original.UserId = null;

        CrashReport copy = CrashXmlSerializer.Deserialize<CrashReport>(CrashXmlSerializer.Serialize(original));

        Assert.IsNull(copy.UserId);
    }

    /// <summary>
    /// The XML is UTF-8 without a byte order mark and without xsi/xsd namespaces.
    /// </summary>
    [TestMethod]
    public void XmlIsPlainUtf8()
    {
        byte[] bytes = CrashXmlSerializer.Serialize(NewReport());
        string text = Encoding.UTF8.GetString(bytes);

        Assert.AreEqual((byte)'<', bytes[0]);
        Assert.Contains("<CrashReport schemaVersion=\"1\">", text);
        Assert.DoesNotContain("xmlns", text);
    }

    /// <summary>
    /// A group survives a round trip.
    /// </summary>
    [TestMethod]
    public void GroupRoundTrips()
    {
        CrashReport report = NewReport();
        CrashGroup original = CrashGroup.FromReport(CrashFingerprinter.Analyze(report), report);

        CrashGroup copy = CrashXmlSerializer.Deserialize<CrashGroup>(CrashXmlSerializer.Serialize(original));

        Assert.AreEqual(original.Fingerprint, copy.Fingerprint);
        Assert.AreEqual(original.Category, copy.Category);
        Assert.AreEqual(original.Module, copy.Module);
        Assert.AreEqual(1, copy.Count);
        Assert.AreEqual(original.LastSeenUtc, copy.LastSeenUtc.ToUniversalTime());
        Assert.AreEqual(report.Id, copy.RecentReportIds[0]);
    }

    /// <summary>
    /// Invalid XML is reported as InvalidDataException.
    /// </summary>
    [TestMethod]
    public void BadXmlThrowsInvalidDataException()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => CrashXmlSerializer.Deserialize<CrashReport>(Encoding.UTF8.GetBytes("<CrashReport><Id>")));
        Assert.ThrowsExactly<InvalidDataException>(() => CrashXmlSerializer.Deserialize<CrashReport>(Encoding.UTF8.GetBytes("<Other/>")));
        Assert.ThrowsExactly<InvalidDataException>(() => CrashXmlSerializer.Deserialize<CrashReport>([]));
    }

    /// <summary>
    /// A DTD in the input is rejected instead of being processed.
    /// </summary>
    [TestMethod]
    public void DtdIsRejected()
    {
        string xml = "<?xml version=\"1.0\"?><!DOCTYPE CrashReport [<!ENTITY x \"boom\">]><CrashReport><Source>&x;</Source></CrashReport>";

        Assert.ThrowsExactly<InvalidDataException>(() => CrashXmlSerializer.Deserialize<CrashReport>(Encoding.UTF8.GetBytes(xml)));
    }

    /// <summary>
    /// Null arguments are rejected.
    /// </summary>
    [TestMethod]
    public void NullArgumentsThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => CrashXmlSerializer.Serialize<CrashReport>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => CrashXmlSerializer.Deserialize<CrashReport>(null!));
    }
}
