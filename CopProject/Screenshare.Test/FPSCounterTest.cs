using ScreenShare;
using ScreenShare.Models;

namespace ScreenShare.Tests;

public class FpsCounterTest
{
    [Fact]
    public void NewUserShows_0_FPS()
    {
        FpsCounter counter = new();
        Assert.Equal(0, counter.FramesPerSecond);
    }

    [Fact]
    public void FirstSampleOnlyStartsTheClock()
    {
        FpsCounter counter = new();
        for (int i = 0; i < 10; i++)
        {
            counter.RecordFrame();
        }

        double fps = counter.Sample(TimeSpan.FromSeconds(0));

        Assert.Equal(0, fps);
        Assert.Equal(0, counter.FramesPerSecond);
    }

    [Fact]
    public void FrameRateDevidedByElaspsedTime()
    {
        FpsCounter counter = new();
        _ = counter.Sample(TimeSpan.FromSeconds(0));
        for (int i = 0; i < 10; i++)
        {
            counter.RecordFrame();
        }

        double fps = counter.Sample(TimeSpan.FromSeconds(2));

        Assert.Equal(5, fps);
        Assert.Equal(5,counter.FramesPerSecond);
    }

    [Fact]
    public void NoFrameRateMeansZero()
    {
        FpsCounter counter = new();
        _ = counter.Sample(TimeSpan.FromSeconds(0));
        for (int i = 0; i < 10; i++)
        {
            counter.RecordFrame();
        }
        _ = counter.Sample(TimeSpan.FromSeconds(1));
        double fps = counter.Sample(TimeSpan.FromSeconds(2));

        Assert.Equal(0, fps);
        Assert.Equal(0, counter.FramesPerSecond);
    }

    [Fact]
    public void TimeNotMovingForwardKeepsLastFrameRate()
    {
        FpsCounter counter = new();
        _ = counter.Sample(TimeSpan.FromSeconds(0));

        for(int i = 0; i < 10; i++)
        {
            counter.RecordFrame();
        }

        _ = counter.Sample(TimeSpan.FromSeconds(1));

        for (int i = 0; i < 5; i++)
        {
            counter.RecordFrame();
        }

        double fps2 = counter.Sample(TimeSpan.FromSeconds(1));

        Assert.Equal(10, fps2);
        Assert.Equal(10, counter.FramesPerSecond);

        for(int i = 0; i < 10; i++)
        {
            counter.RecordFrame();
        }

        double fps3 = counter.Sample(TimeSpan.FromSeconds(3));

        Assert.Equal(5, fps3);
        Assert.Equal(5, counter.FramesPerSecond);


    }

    [Fact]
    public void ResetClearsEverything()
    {
        FpsCounter counter = new();
        counter.Sample(TimeSpan.FromSeconds(0));

        for (int i = 0; i < 10; i++)
        {
            counter.RecordFrame();
        }

        double fps = counter.Sample(TimeSpan.FromSeconds(1));
        counter.Reset();

        Assert.Equal(10, fps); // before calling reset it should 10 
        Assert.Equal(0, counter.FramesPerSecond);
    }
}
