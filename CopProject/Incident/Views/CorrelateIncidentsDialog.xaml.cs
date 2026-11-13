
using System.Diagnostics.CodeAnalysis;
using Incidents.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Incidents.Views;

[ExcludeFromCodeCoverage]
public sealed partial class CorrelateIncidentsDialog : ContentDialog
{
    /// <summary>
    /// Creates the CorrelateIncidentsDialog and loads its XAML layout.
    /// </summary>
    public CorrelateIncidentsDialog()
    {
        InitializeComponent();
    }
}
