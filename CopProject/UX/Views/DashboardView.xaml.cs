using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class DashboardView : UserControl
{
    public DashboardViewModel ViewModel { get; set; } = null!;

    public DashboardView()
    {
        InitializeComponent();
    }

    async void Join_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearJoinForm();
        await new JoinDialog(ViewModel) { XamlRoot = XamlRoot }.ShowAsync();
    }
}
