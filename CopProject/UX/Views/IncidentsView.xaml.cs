using Microsoft.UI.Xaml.Controls;
using SecureMeet.Shared.Modules;
using UX.Models;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class IncidentsView : UserControl
{
    public ShellViewModel ViewModel { get; set; } = null!;

    public IncidentsView()
    {
        InitializeComponent();
    }

    public void AttachModules(ModuleCatalog modules)
    {
        IncidentsHost.Content = modules.ViewFor(ModuleSlot.Incidents);
    }
}
