using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureMeet.Shared.Modules;
using UX.Models;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class MeetingView : UserControl
{
    public MeetingViewModel ViewModel { get; set; } = null!;

    public MeetingView()
    {
        InitializeComponent();
    }

    public void AttachModules(ModuleCatalog modules)
    {
        VideoHost.Content = modules.ViewFor(ModuleSlot.Video);
        ShareHost.Content = modules.ViewFor(ModuleSlot.Share);
        BoardHost.Content = modules.ViewFor(ModuleSlot.Whiteboard);
        ChatHost.Content = modules.ViewFor(ModuleSlot.Chat);
    }

    async void End_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = ViewModel.EndButtonText + "?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            ViewModel.EndMeeting();
    }
}
