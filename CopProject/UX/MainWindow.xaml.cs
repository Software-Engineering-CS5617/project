using Microsoft.UI.Xaml;
using UX.Models;
using UX.ViewModels;

namespace UX;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "SecureMeet";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
    }

    public void Attach(ShellViewModel viewModel, ModuleCatalog modules) => ShellPane.Attach(viewModel, modules);
}
