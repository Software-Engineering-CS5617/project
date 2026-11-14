using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Cloud.Networking;
using Cloud.UI.Models;
using Cloud.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Cloud.UI.Views;

[ExcludeFromCodeCoverage]
/// <summary>
/// Notification Service page. The logic lives in <see cref="NotificationServiceViewModel"/>;
/// this class only handles view-only concerns: status colour, Enter key, and the alert popup.
/// </summary>
public sealed partial class NotificationServiceView : UserControl
{
    private static readonly SolidColorBrush s_connected = new(Colors.SeaGreen);
    private static readonly SolidColorBrush s_pending = new(Colors.DarkOrange);
    private static readonly SolidColorBrush s_disconnected = new(Colors.Gray);

    // WinUI allows only one ContentDialog at a time, so alerts are shown one after another.
    private readonly SemaphoreSlim _dialogGate = new(1, 1);

    public NotificationServiceView(NotificationServiceViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        // must be set before InitializeComponent;
        // x:Bind reads it while loading the XAML
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NotificationReceived += OnNotificationReceived;
    }

    /// <summary>The view model the XAML binds to.</summary>
    public NotificationServiceViewModel ViewModel { get; }

    /// <summary>Colour of the status dot for a connection state.</summary>
    public Brush GetStateBrush(ConnectionState state)
    {
        return state switch {
            ConnectionState.Connected => s_connected,
            ConnectionState.Connecting or ConnectionState.Reconnecting => s_pending,
            _ => s_disconnected
        };
    }

    private void ServerUrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Enter connects, or switches to the typed server when already connected.
        ICommand command = ViewModel.ConnectCommand.CanExecute(null) ? ViewModel.ConnectCommand : ViewModel.ReconnectCommand;
        RunOnEnter(e, command);
    }

    private void MessageBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        RunOnEnter(e, ViewModel.SendCommand);
    }

    private static void RunOnEnter(KeyRoutedEventArgs e, ICommand command)
    {
        if (e.Key != VirtualKey.Enter)
        {
            return;
        }

        if (command.CanExecute(null))
        {
            command.Execute(null);
        }

        e.Handled = true;
    }

    private async void OnNotificationReceived(NotificationItem item)
    {
        await _dialogGate.WaitAsync();
        try
        {
            var dialog = new ContentDialog {
                Title = "New notification",
                Content = new TextBlock {
                    Text = $"{item.Content}\n\nFrom {item.SenderId} · {item.TimeText}",
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // The notification is already in the list; a failed popup must never crash the app.
            Debug.WriteLine($"Notification alert failed: {ex}");
        }
        finally
        {
            _dialogGate.Release();
        }
    }
}
