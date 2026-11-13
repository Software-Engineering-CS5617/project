
using System.ComponentModel;
namespace Incidents.ViewModels.Utils;

/// <summary>
/// Base class for ViewModels that notifies the UI when a property changes.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Raised when a property value changes so the UI can refresh.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Tells the UI that the named property has changed.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
