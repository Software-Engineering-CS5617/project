using Microsoft.UI.Xaml.Controls;
using Controller.ViewModels;

namespace Controller.Views;

public sealed partial class MeetingView : UserControl
{
    public MeetingViewModel ViewModel { get; set; } = null!;

    public MeetingView()
    {
        InitializeComponent();
    }
}