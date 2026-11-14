using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests text cleaning for crash reports.
/// </summary>
[TestClass]
public class TestCrashText
{
    /// <summary>
    /// Valid text is returned unchanged.
    /// </summary>
    [TestMethod]
    public void SanitizeKeepsValidText()
    {
        string text = "line1\r\nline2\ttab \U0001F44D";
        Assert.AreEqual(text, CrashText.Sanitize(text));
    }

    /// <summary>
    /// Control characters and lone surrogates are removed.
    /// </summary>
    [TestMethod]
    public void SanitizeRemovesInvalidXmlCharacters()
    {
        Assert.AreEqual("ab", CrashText.Sanitize("a\u0000\u0001b"));
        Assert.AreEqual("ab", CrashText.Sanitize("a\uD800b"));
        Assert.AreEqual("ab", CrashText.Sanitize("a\uDC00b"));
    }

    /// <summary>
    /// Null and empty text become an empty string.
    /// </summary>
    [TestMethod]
    public void SanitizeHandlesNullAndEmpty()
    {
        Assert.AreEqual(string.Empty, CrashText.Sanitize(null));
        Assert.AreEqual(string.Empty, CrashText.Sanitize(string.Empty));
    }

    /// <summary>
    /// User names in Windows and Unix profile paths are replaced.
    /// </summary>
    [TestMethod]
    public void ScrubPathsReplacesUserNames()
    {
        Assert.AreEqual(
            @"at X in C:\Users\<user>\src\a.cs:line 5",
            CrashText.ScrubPaths(@"at X in C:\Users\alice\src\a.cs:line 5"));
        Assert.AreEqual(
            @"c:\users\<user>\x",
            CrashText.ScrubPaths(@"c:\users\John Smith\x"));
        Assert.AreEqual("/home/<user>/code", CrashText.ScrubPaths("/home/bob/code"));
        Assert.AreEqual("/Users/<user>/code", CrashText.ScrubPaths("/Users/bob/code"));
    }

    /// <summary>
    /// Text without profile paths is unchanged, and null becomes empty.
    /// </summary>
    [TestMethod]
    public void ScrubPathsLeavesOtherTextAlone()
    {
        Assert.AreEqual(@"C:\Windows\System32\x.dll", CrashText.ScrubPaths(@"C:\Windows\System32\x.dll"));
        Assert.AreEqual(string.Empty, CrashText.ScrubPaths(null));
    }

    /// <summary>
    /// Long text is cut and marked, short text is kept.
    /// </summary>
    [TestMethod]
    public void TruncateCutsLongText()
    {
        Assert.AreEqual("abcdefg...", CrashText.Truncate("abcdefghijklmnop", 10));
        Assert.AreEqual("short", CrashText.Truncate("short", 10));
        Assert.AreEqual(string.Empty, CrashText.Truncate(null, 10));
    }

    /// <summary>
    /// A surrogate pair is never split by truncation.
    /// </summary>
    [TestMethod]
    public void TruncateDoesNotSplitSurrogatePair()
    {
        string text = "abcd\U0001F44Defgh";
        string result = CrashText.Truncate(text, 8);

        Assert.AreEqual("abcd...", result);
    }

    /// <summary>
    /// A limit below four characters is rejected.
    /// </summary>
    [TestMethod]
    public void TruncateRejectsTinyLimit()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CrashText.Truncate("abcdef", 3));
    }
}
