using System.Windows;

namespace MoniHop.Desktop.Views;

public partial class DisplayRenameDialog : Window
{
    public DisplayRenameDialog(string currentName)
    {
        InitializeComponent();
        NameTextBox.Text = currentName;
        NameTextBox.SelectAll();
        NameTextBox.Focus();
    }

    public string DisplayName => NameTextBox.Text.Trim();

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            MessageBox.Show(this, "请输入显示器名称。", "MoniHop", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }
}
