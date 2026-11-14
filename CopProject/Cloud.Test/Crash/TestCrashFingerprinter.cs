using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests fingerprinting and classification of crash reports.
/// </summary>
[TestClass]
public class TestCrashFingerprinter
{
    private static CrashFrame Frame(string type, string method, string assembly = "Chat", int line = 0)
    {
        return new CrashFrame { DeclaringType = type, Method = method, Assembly = assembly, Line = line, FileName = line > 0 ? "File.cs" : null };
    }

    private static CrashReport Report(string exceptionType, params CrashFrame[] frames)
    {
        return new CrashReport {
            Exceptions = [new CrashExceptionInfo { Type = exceptionType, Message = "m", Frames = [.. frames] }]
        };
    }

    private static CrashReport DefaultReport(int line = 10)
    {
        return Report(
            "System.InvalidOperationException",
            Frame("Chat.ChatServer", "HandleOperation", line: line),
            Frame("Chat.ChatServer", "HandleRequest", line: line + 5),
            Frame("Networking.ReceivingQueueListener", "ListenOnQueue", "Networking"));
    }

    /// <summary>
    /// The fingerprint is a 16 character lowercase hex string.
    /// </summary>
    [TestMethod]
    public void FingerprintIsSixteenHexCharacters()
    {
        string fingerprint = CrashFingerprinter.Analyze(DefaultReport()).Fingerprint;

        Assert.AreEqual(16, fingerprint.Length);
        Assert.IsTrue(fingerprint.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)));
    }

    /// <summary>
    /// The same crash gives the same fingerprint regardless of line numbers, message, user and time.
    /// </summary>
    [TestMethod]
    public void SameCrashWithDifferentDetailsHasSameFingerprint()
    {
        CrashReport first = DefaultReport(line: 10);
        CrashReport second = DefaultReport(line: 99);
        second.Exceptions[0].Message = "a completely different message";
        second.UserId = "someone-else";
        second.TimestampUtc = DateTime.UtcNow.AddDays(3);

        Assert.AreEqual(CrashFingerprinter.Analyze(first).Fingerprint, CrashFingerprinter.Analyze(second).Fingerprint);
    }

    /// <summary>
    /// A different exception type or a different top frame gives a different fingerprint.
    /// </summary>
    [TestMethod]
    public void DifferentTypeOrTopFrameHasDifferentFingerprint()
    {
        string baseline = CrashFingerprinter.Analyze(DefaultReport()).Fingerprint;

        CrashReport otherType = DefaultReport();
        otherType.Exceptions[0].Type = "System.NullReferenceException";

        CrashReport otherFrame = DefaultReport();
        otherFrame.Exceptions[0].Frames[0].Method = "SomethingElse";

        Assert.AreNotEqual(baseline, CrashFingerprinter.Analyze(otherType).Fingerprint);
        Assert.AreNotEqual(baseline, CrashFingerprinter.Analyze(otherFrame).Fingerprint);
    }

    /// <summary>
    /// Only the top frames take part, so deeper frames do not change the fingerprint.
    /// </summary>
    [TestMethod]
    public void FramesBeyondTheLimitAreIgnored()
    {
        List<CrashFrame> frames = [];
        for (int i = 0; i < CrashFingerprinter.MaxFrames; i++)
        {
            frames.Add(Frame("Chat.Type" + i, "Method"));
        }

        CrashReport shallow = Report("System.Exception", [.. frames]);
        frames.Add(Frame("Chat.Deep", "Method"));
        CrashReport deep = Report("System.Exception", [.. frames]);

        Assert.AreEqual(CrashFingerprinter.Analyze(shallow).Fingerprint, CrashFingerprinter.Analyze(deep).Fingerprint);
    }

    /// <summary>
    /// Recursion depth does not change the fingerprint.
    /// </summary>
    [TestMethod]
    public void RepeatedFramesAreCollapsed()
    {
        CrashReport once = Report("System.Exception", Frame("Chat.A", "Run"), Frame("Chat.B", "Start"));
        CrashReport recursive = Report(
            "System.Exception",
            Frame("Chat.A", "Run"),
            Frame("Chat.A", "Run"),
            Frame("Chat.A", "Run"),
            Frame("Chat.B", "Start"));

        Assert.AreEqual(CrashFingerprinter.Analyze(once).Fingerprint, CrashFingerprinter.Analyze(recursive).Fingerprint);
    }

    /// <summary>
    /// Framework frames above the application frames do not affect the fingerprint or the module.
    /// </summary>
    [TestMethod]
    public void FrameworkFramesAreSkippedWhenApplicationFramesExist()
    {
        CrashReport plain = Report("System.Exception", Frame("Chat.A", "Run"));
        CrashReport withFramework = Report(
            "System.Exception",
            Frame("System.IO.File", "ReadAllBytes", "System.Private.CoreLib"),
            Frame("Chat.A", "Run"));

        CrashSignature signature = CrashFingerprinter.Analyze(withFramework);

        Assert.AreEqual(CrashFingerprinter.Analyze(plain).Fingerprint, signature.Fingerprint);
        Assert.AreEqual("Chat.A.Run", signature.TopFrame);
        Assert.AreEqual("Chat", signature.Module);
    }

    /// <summary>
    /// With no application frames, framework frames are used and the module is unknown.
    /// </summary>
    [TestMethod]
    public void OnlyFrameworkFramesFallBackToAllFrames()
    {
        CrashReport report = Report("System.IO.IOException", Frame("System.IO.File", "ReadAllBytes", "System.Private.CoreLib"));

        CrashSignature signature = CrashFingerprinter.Analyze(report);

        Assert.AreEqual("System.IO.File.ReadAllBytes", signature.TopFrame);
        Assert.AreEqual(ModuleCatalog.UnknownModule, signature.Module);
        Assert.AreEqual(CrashCategory.FileSystem, signature.Category);
    }

    /// <summary>
    /// Lambdas and async state machines do not change the fingerprint when they are renumbered.
    /// </summary>
    [TestMethod]
    public void CompilerGeneratedNamesAreStable()
    {
        CrashReport first = Report("System.Exception", Frame("Chat.A+<Run>d__3", "MoveNext"), Frame("Chat.A+<>c", "<Start>b__1_0"));
        CrashReport second = Report("System.Exception", Frame("Chat.A+<Run>d__9", "MoveNext"), Frame("Chat.A+<>c__DisplayClass4_0", "<Start>b__2"));

        Assert.AreEqual(CrashFingerprinter.Analyze(first).Fingerprint, CrashFingerprinter.Analyze(second).Fingerprint);
    }

    /// <summary>
    /// The fingerprint uses the root cause, so wrapper exceptions do not split groups.
    /// </summary>
    [TestMethod]
    public void RootCauseIsUsedInsteadOfWrapper()
    {
        CrashReport direct = Report("System.IO.IOException", Frame("Chat.A", "Run"));
        CrashReport wrapped = new() {
            Exceptions = [
                new CrashExceptionInfo { Type = "System.AggregateException", Frames = [Frame("Chat.Wrapper", "Wait")] },
                new CrashExceptionInfo { Type = "System.IO.IOException", Frames = [Frame("Chat.A", "Run")] },
                new CrashExceptionInfo { Type = "System.Exception", Frames = [] }
            ]
        };

        CrashSignature signature = CrashFingerprinter.Analyze(wrapped);

        Assert.AreEqual(CrashFingerprinter.Analyze(direct).Fingerprint, signature.Fingerprint);
        Assert.AreEqual("System.IO.IOException", signature.ExceptionType);
    }

    /// <summary>
    /// When no exception has frames, the innermost exception is used.
    /// </summary>
    [TestMethod]
    public void WithoutAnyFramesTheInnermostExceptionIsUsed()
    {
        CrashReport report = new() {
            Exceptions = [
                new CrashExceptionInfo { Type = "System.Exception" },
                new CrashExceptionInfo { Type = "System.TimeoutException" }
            ]
        };

        CrashSignature signature = CrashFingerprinter.Analyze(report);

        Assert.AreEqual("System.TimeoutException", signature.ExceptionType);
        Assert.AreEqual(CrashFingerprinter.NoStackFrame, signature.TopFrame);
        Assert.AreEqual(CrashCategory.Network, signature.Category);
    }

    /// <summary>
    /// A report without exceptions is analysed as unknown with a stable fingerprint.
    /// </summary>
    [TestMethod]
    public void EmptyReportIsUnknownAndStable()
    {
        CrashSignature first = CrashFingerprinter.Analyze(new CrashReport());
        CrashSignature second = CrashFingerprinter.Analyze(new CrashReport());

        Assert.AreEqual(CrashFingerprinter.UnknownExceptionType, first.ExceptionType);
        Assert.AreEqual(CrashCategory.Other, first.Category);
        Assert.AreEqual(first.Fingerprint, second.Fingerprint);
        Assert.IsNull(CrashFingerprinter.GetRootCause(new CrashReport()));
    }

    /// <summary>
    /// A blank exception type is treated as unknown.
    /// </summary>
    [TestMethod]
    public void BlankExceptionTypeIsUnknown()
    {
        CrashReport report = Report(" ", Frame("Chat.A", "Run"));

        Assert.AreEqual(CrashFingerprinter.UnknownExceptionType, CrashFingerprinter.Analyze(report).ExceptionType);
    }

    /// <summary>
    /// The signature reports the top frame, module and category.
    /// </summary>
    [TestMethod]
    public void SignatureDescribesTheCrash()
    {
        CrashSignature signature = CrashFingerprinter.Analyze(DefaultReport());

        Assert.AreEqual("System.InvalidOperationException", signature.ExceptionType);
        Assert.AreEqual("Chat.ChatServer.HandleOperation", signature.TopFrame);
        Assert.AreEqual("Chat", signature.Module);
        Assert.AreEqual(CrashCategory.InvalidState, signature.Category);
    }

    /// <summary>
    /// A null report is rejected.
    /// </summary>
    [TestMethod]
    public void NullReportThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => CrashFingerprinter.Analyze(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => CrashFingerprinter.GetRootCause(null!));
    }
}
