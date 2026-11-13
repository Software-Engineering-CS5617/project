using System.ComponentModel;
using Incidents.ViewModels.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Testing ViewModelBase property change notification functionality.
/// </summary>
[TestClass]
public sealed class ViewModelBaseTest
{
    private sealed class TestViewModel : ViewModelBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set {
                if (_name == value) { return; }

                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// PropertyChanged event is raised when a property changes.
    /// </summary>
    [TestMethod]
    public void OnPropertyChanged_RaisesPropertyChangedEvent()
    {
        TestViewModel viewModel = new();
        string? changedProperty = null;
        viewModel.PropertyChanged += (_, e) => {
            changedProperty = e.PropertyName;
        };
        viewModel.Name = "Test";

        Assert.AreEqual(nameof(TestViewModel.Name), changedProperty);
    }

    /// <summary>
    /// PropertyChanged event provides the correct property name.
    /// </summary>
    [TestMethod]
    public void OnPropertyChanged_ReturnsCorrectPropertyName()
    {
        TestViewModel viewModel = new();
        PropertyChangedEventArgs? eventArgs = null;
        viewModel.PropertyChanged += (_, e) => {
            eventArgs = e;
        };
        viewModel.Name = "Incident";

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual("Name", eventArgs.PropertyName);
    }

    /// <summary>
    /// Setting a property to its existing value does not raise an event.
    /// </summary>
    [TestMethod]
    public void PropertySetter_SameValue_DoesNotRaiseEvent()
    {
        TestViewModel viewModel = new() { Name = "Incident" };
        int eventCount = 0;
        viewModel.PropertyChanged += (_, _) => eventCount++;
        viewModel.Name = "Incident";

        Assert.AreEqual(0, eventCount);
    }
}
