using Microsoft.UI.Xaml.Controls;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class JoinDialog : ContentDialog
{
    public DashboardViewModel ViewModel { get; }

    public JoinDialog(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    void Join_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!ViewModel.TryJoin()) args.Cancel = true;
    }
}
