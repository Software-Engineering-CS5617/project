// Author : Eswar

using FileSynchroniser.Views;
using Microsoft.UI.Xaml;

namespace FileSynchroniser.UIharness;
/// <summary>
/// The FileSync Page is added to Main Window
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        RootFrame.Navigate(typeof(FileSyncPage));
    }
}
