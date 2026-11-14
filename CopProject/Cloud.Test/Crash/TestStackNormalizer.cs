using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests that frames are reduced to stable names.
/// </summary>
[TestClass]
public class TestStackNormalizer
{
    private static CrashFrame Frame(string type, string method)
    {
        return new CrashFrame { DeclaringType = type, Method = method, Assembly = "Chat" };
    }

    /// <summary>
    /// A plain method keeps its namespace, type and name.
    /// </summary>
    [TestMethod]
    public void PlainMethodIsNamespaceTypeMethod()
    {
        Assert.AreEqual("Chat.ChatServer.HandleRequest", StackNormalizer.NormalizeFrame(Frame("Chat.ChatServer", "HandleRequest")));
    }

    /// <summary>
    /// Constructors keep their special names.
    /// </summary>
    [TestMethod]
    public void ConstructorNameIsKept()
    {
        Assert.AreEqual("Chat.Chatroom..ctor", StackNormalizer.NormalizeFrame(Frame("Chat.Chatroom", ".ctor")));
    }

    /// <summary>
    /// The generic arity marker is removed from type names.
    /// </summary>
    [TestMethod]
    public void GenericArityIsRemoved()
    {
        Assert.AreEqual("Cloud.Box.Get", StackNormalizer.NormalizeFrame(Frame("Cloud.Box`2", "Get")));
    }

    /// <summary>
    /// Constructed generic type arguments are removed.
    /// </summary>
    [TestMethod]
    public void ConstructedGenericArgumentsAreRemoved()
    {
        string type = "Cloud.Box`1[[System.String, System.Private.CoreLib, Version=10.0.0.0]]";
        Assert.AreEqual("Cloud.Box.Get", StackNormalizer.NormalizeFrame(Frame(type, "Get")));
    }

    /// <summary>
    /// An async state machine's MoveNext is reported as the original method.
    /// </summary>
    [TestMethod]
    public void AsyncStateMachineMapsToOriginalMethod()
    {
        Assert.AreEqual(
            "Incident_module.IncidentController.CreateIncident",
            StackNormalizer.NormalizeFrame(Frame("Incident_module.IncidentController+<CreateIncident>d__7", "MoveNext")));
    }

    /// <summary>
    /// The numeric suffix of the state machine does not matter.
    /// </summary>
    [TestMethod]
    public void StateMachineSuffixDoesNotChangeName()
    {
        string first = StackNormalizer.NormalizeFrame(Frame("A.B+<Run>d__3", "MoveNext"));
        string second = StackNormalizer.NormalizeFrame(Frame("A.B+<Run>d__12", "MoveNext"));

        Assert.AreEqual(first, second);
    }

    /// <summary>
    /// A lambda in a closure class is reported as its containing method.
    /// </summary>
    [TestMethod]
    public void LambdaInClosureMapsToContainingMethod()
    {
        Assert.AreEqual("Chat.ChatServer.Run", StackNormalizer.NormalizeFrame(Frame("Chat.ChatServer+<>c__DisplayClass3_0", "<Run>b__0")));
        Assert.AreEqual("Chat.ChatServer.Run", StackNormalizer.NormalizeFrame(Frame("Chat.ChatServer+<>c", "<Run>b__2_1")));
    }

    /// <summary>
    /// A lambda nested in a lambda is reported as the outer method.
    /// </summary>
    [TestMethod]
    public void NestedLambdaMapsToOutermostMethod()
    {
        Assert.AreEqual("Chat.ChatServer.Run", StackNormalizer.NormalizeFrame(Frame("Chat.ChatServer+<>c", "<<Run>b__0>b__1")));
    }

    /// <summary>
    /// A lambda inside an async method maps to that async method.
    /// </summary>
    [TestMethod]
    public void LambdaInsideAsyncMethodMapsToAsyncMethod()
    {
        Assert.AreEqual("A.B.Run", StackNormalizer.NormalizeFrame(Frame("A.B+<<Run>b__0>d__4", "MoveNext")));
    }

    /// <summary>
    /// A local function keeps its own name under the containing method.
    /// </summary>
    [TestMethod]
    public void LocalFunctionIsReportedUnderContainingMethod()
    {
        Assert.AreEqual("A.B.Run.Helper", StackNormalizer.NormalizeFrame(Frame("A.B", "<Run>g__Helper|2_0")));
    }

    /// <summary>
    /// A MoveNext without a state machine type keeps its name.
    /// </summary>
    [TestMethod]
    public void MoveNextOnNormalTypeIsKept()
    {
        Assert.AreEqual("A.Enumerator.MoveNext", StackNormalizer.NormalizeFrame(Frame("A.Enumerator", "MoveNext")));
    }

    /// <summary>
    /// A frame without a declaring type is just the method.
    /// </summary>
    [TestMethod]
    public void EmptyTypeGivesMethodOnly()
    {
        Assert.AreEqual("Main", StackNormalizer.NormalizeFrame(Frame(string.Empty, "Main")));
    }

    /// <summary>
    /// A null frame is rejected.
    /// </summary>
    [TestMethod]
    public void NullFrameThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => StackNormalizer.NormalizeFrame(null!));
    }
}
