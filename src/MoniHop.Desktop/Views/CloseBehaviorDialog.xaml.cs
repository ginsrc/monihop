using System.Windows;
using MoniHop.Desktop.Lifecycle;

namespace MoniHop.Desktop.Views;

public partial class CloseBehaviorDialog : Window
{
    public CloseBehaviorDialog()
    {
        InitializeComponent();
    }

    public ClosePromptResult? Result { get; private set; }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        Result = new ClosePromptResult(CloseAction.Hide, RememberChoiceCheckBox.IsChecked == true);
        DialogResult = true;
    }

    private void ExitButton_OnClick(object sender, RoutedEventArgs e)
    {
        Result = new ClosePromptResult(CloseAction.Exit, RememberChoiceCheckBox.IsChecked == true);
        DialogResult = true;
    }
}
