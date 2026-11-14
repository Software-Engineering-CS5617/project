using System.Diagnostics;
using ScreenShare;

namespace Screenshare.Tests;

/// <summary>
/// tests for the worker thread that runs ScreenShare's work one item at a time. every wait has a
/// timeout, so a broken dispatcher fails the test instead of hanging the run.
/// </summary>
public class SerialDispatcherTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    private static void WaitFor(ManualResetEventSlim signal)
    {
        Assert.True(signal.Wait(s_timeout), "the work item did not run in time");
    }

    private static void WaitUntilIdle(SerialDispatcher dispatcher)
    {
        Assert.True(SpinWait.SpinUntil(() => dispatcher.IsIdle, s_timeout), "the dispatcher did not become idle");
    }

    [Fact]
    public void Post_NullAction_Throws()
    {
        using SerialDispatcher dispatcher = new("test");

        Assert.Throws<ArgumentNullException>(() => dispatcher.Post(null!));
    }

    [Fact]
    public void Post_RunsActionOnNamedBackgroundThread()
    {
        using SerialDispatcher dispatcher = new("ScreenShare test");
        using ManualResetEventSlim ran = new();
        Thread? worker = null;

        dispatcher.Post(() => {
            worker = Thread.CurrentThread;
            ran.Set();
        });

        WaitFor(ran);
        Assert.NotSame(Thread.CurrentThread, worker);
        Assert.Equal("ScreenShare test", worker!.Name);
        Assert.True(worker.IsBackground);
    }

    [Fact]
    public void Post_RunsActionsInOrder()
    {
        using SerialDispatcher dispatcher = new("test");
        using ManualResetEventSlim done = new();
        List<int> order = new();

        for (int i = 0; i < 100; i++)
        {
            int number = i;
            dispatcher.Post(() => order.Add(number));
        }
        dispatcher.Post(done.Set);

        WaitFor(done);
        Assert.Equal(Enumerable.Range(0, 100), order);
    }

    [Fact]
    public void Post_FromManyThreads_RunsOneAtATime()
    {
        using SerialDispatcher dispatcher = new("test");
        int running = 0;
        int mostAtOnce = 0;
        int finished = 0;

        Parallel.For(0, 200, _ => dispatcher.Post(() => {
            int now = Interlocked.Increment(ref running);
            InterlockedMax(ref mostAtOnce, now);
            Thread.SpinWait(1000);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref finished);
        }));

        WaitUntilIdle(dispatcher);
        Assert.Equal(200, Volatile.Read(ref finished));
        Assert.Equal(1, Volatile.Read(ref mostAtOnce));
    }

    [Fact]
    public void IsIdle_NewDispatcher_IsTrue()
    {
        using SerialDispatcher dispatcher = new("test");

        Assert.True(dispatcher.IsIdle);
    }

    [Fact]
    public void IsIdle_WhileWorkRunsOrWaits_IsFalse()
    {
        using SerialDispatcher dispatcher = new("test");
        using ManualResetEventSlim started = new();
        using ManualResetEventSlim release = new();

        dispatcher.Post(() => {
            started.Set();
            release.Wait(s_timeout);
        });
        dispatcher.Post(() => { });   // queued behind the first item
        WaitFor(started);

        Assert.False(dispatcher.IsIdle);

        release.Set();
        WaitUntilIdle(dispatcher);
    }

    [Fact]
    public void IsIdle_CountsWorkPostedByWorkItem()
    {
        using SerialDispatcher dispatcher = new("test");
        using ManualResetEventSlim firstDone = new();
        using ManualResetEventSlim release = new();

        dispatcher.Post(() => {
            dispatcher.Post(() => release.Wait(s_timeout));
            firstDone.Set();
        });
        WaitFor(firstDone);

        // the follow-up item is counted even though the item that posted it has finished
        Assert.False(dispatcher.IsIdle);

        release.Set();
        WaitUntilIdle(dispatcher);
    }

    [Fact]
    public void Post_ActionThrows_LaterWorkStillRuns()
    {
        using SerialDispatcher dispatcher = new("test");
        using ManualResetEventSlim ran = new();

        dispatcher.Post(() => throw new InvalidOperationException("bad message"));
        dispatcher.Post(ran.Set);

        WaitFor(ran);
        WaitUntilIdle(dispatcher);
    }

    [Fact]
    public void Dispose_FinishesWorkAlreadyQueued()
    {
        SerialDispatcher dispatcher = new("test");
        int ran = 0;

        for (int i = 0; i < 50; i++)
        {
            dispatcher.Post(() => Interlocked.Increment(ref ran));
        }
        dispatcher.Dispose();

        Assert.Equal(50, Volatile.Read(ref ran));
        Assert.True(dispatcher.IsIdle);
    }

    [Fact]
    public void Post_AfterDispose_IsDropped()
    {
        SerialDispatcher dispatcher = new("test");
        dispatcher.Dispose();
        bool ran = false;

        dispatcher.Post(() => ran = true);

        Assert.True(dispatcher.IsIdle);
        Assert.False(ran);
    }

    [Fact]
    public void Dispose_Twice_IsSafe()
    {
        SerialDispatcher dispatcher = new("test");

        dispatcher.Dispose();
        dispatcher.Dispose();

        Assert.True(dispatcher.IsIdle);
    }

    [Fact]
    public void Dispose_FromOwnWorkItem_DoesNotWaitForItself()
    {
        SerialDispatcher dispatcher = new("test");
        using ManualResetEventSlim done = new();
        TimeSpan disposeTook = TimeSpan.Zero;

        dispatcher.Post(() => {
            Stopwatch watch = Stopwatch.StartNew();
            dispatcher.Dispose();
            disposeTook = watch.Elapsed;
            done.Set();
        });

        WaitFor(done);
        // waiting for its own thread would only give up after the 2 second join timeout
        Assert.True(disposeTook < TimeSpan.FromSeconds(1), $"Dispose took {disposeTook}");
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current)
        {
            int seen = Interlocked.CompareExchange(ref target, value, current);
            if (seen == current)
            {
                return;
            }

            current = seen;
        }
    }
}
