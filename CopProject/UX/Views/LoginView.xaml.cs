using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class LoginView : UserControl
{
    public LoginViewModel ViewModel { get; set; } = null!;

    public LoginView()
    {
        InitializeComponent();
    }

    void Email_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) ViewModel.SignInCommand.Execute(null);
    }

    void Email_LostFocus(object sender, RoutedEventArgs e) => ViewModel.AutofillFromEmail();
}
