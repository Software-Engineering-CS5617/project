using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SecureMeet.Shared.Mvvm;

// Base class of every ViewModel.
// Simplest use: change your properties, then call Refresh() and the whole screen updates.
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // update everything on the screen that is bound to this ViewModel
    protected void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

    // update just one property
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        field = value;
        OnPropertyChanged(name);
    }
}
