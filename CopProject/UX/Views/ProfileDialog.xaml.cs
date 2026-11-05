using Microsoft.UI.Xaml.Controls;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class ProfileDialog : ContentDialog
{
    public ProfileViewModel ViewModel { get; }

    public ProfileDialog(ProfileViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!ViewModel.TrySave()) args.Cancel = true;
    }
}
