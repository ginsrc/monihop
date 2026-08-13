using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MoniHop.Windows.ApplicationProjection;

namespace MoniHop.Desktop.Views;

public partial class RunningApplicationDialog : Window
{
    public RunningApplicationDialog(IReadOnlyList<ApplicationWindowSnapshot> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);
        Applications = new ObservableCollection<ApplicationWindowSnapshot>(applications);
        InitializeComponent();
        DataContext = this;
        if (Applications.Count > 0)
        {
            ApplicationsList.SelectedIndex = 0;
        }
    }

    public ObservableCollection<ApplicationWindowSnapshot> Applications { get; }

    public ApplicationWindowSnapshot? SelectedApplication =>
        ApplicationsList.SelectedItem as ApplicationWindowSnapshot;

    private void Select_OnClick(object sender, RoutedEventArgs e) => AcceptSelection();

    private void ApplicationsList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        AcceptSelection();

    private void AcceptSelection()
    {
        if (SelectedApplication is null)
        {
            MessageBox.Show(this, "请选择一个应用。", "MoniHop", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }
}
