using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UX.Models;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class ShellView : UserControl
{
    public ShellViewModel ViewModel { get; private set; } = null!;

    public ShellView()
    {
        InitializeComponent();
    }

    public void Attach(ShellViewModel viewModel, ModuleCatalog modules)
    {
        ViewModel = viewModel;

        LoginPane.ViewModel = viewModel.Login;
        DashboardPane.ViewModel = viewModel.Dashboard;
        IncidentsPane.ViewModel = viewModel;
        MeetingPane.ViewModel = viewModel.Meeting;

        IncidentsPane.AttachModules(modules);
        MeetingPane.AttachModules(modules);

        Bindings.Update();
    }

    async void Profile_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Profile.Load();
        await new ProfileDialog(ViewModel.Profile) { XamlRoot = XamlRoot }.ShowAsync();
    }
}
