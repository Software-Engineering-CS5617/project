using Cloud.UI.Models;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

[TestClass]
public class NotificationItemTests
{
    private static NotificationItem Create(string sender = "alice", DateTimeOffset? at = null)
    {
        return new() {
            Id = Guid.NewGuid(),
            SenderId = sender,
            Content = "hi",
            ReceivedAt = at ?? new DateTimeOffset(2026, 3, 4, 15, 30, 0, TimeSpan.Zero)
        };
    }

    [TestMethod]
    public void TimeText_IsShortLocalTime()
    {
        NotificationItem item = Create();

        Assert.AreEqual(item.ReceivedAt.ToLocalTime().ToString("t"), item.TimeText);
    }

    [TestMethod]
    public void Subtitle_IsSenderDotTime()
    {
        NotificationItem item = Create("bob");

        Assert.AreEqual("bob · " + item.TimeText, item.Subtitle);
    }

}

[TestClass]
public class ObservableObjectTests
{
    private sealed class Sample : ObservableObject
    {
        private int _value;

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public void Raise(string? name)
        {
            OnPropertyChanged(name);
        }
    }

    [TestMethod]
    public void SetProperty_NewValue_RaisesWithPropertyName()
    {
        Sample sample = new();
        List<string?> names = new();
        sample.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        sample.Value = 5;

        CollectionAssert.AreEqual(new[] { nameof(Sample.Value) }, names);
        Assert.AreEqual(5, sample.Value);
    }

    [TestMethod]
    public void SetProperty_SameValue_DoesNotRaise()
    {
        Sample sample = new() { Value = 5 };
        int raised = 0;
        sample.PropertyChanged += (_, _) => raised++;

        sample.Value = 5;

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void OnPropertyChanged_PassesSenderAndName()
    {
        Sample sample = new();
        object? sender = null;
        string? name = null;
        sample.PropertyChanged += (s, e) => { sender = s; name = e.PropertyName; };

        sample.Raise("Other");

        Assert.AreSame(sample, sender);
        Assert.AreEqual("Other", name);
    }

}
