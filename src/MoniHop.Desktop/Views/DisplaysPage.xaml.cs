using System.Collections.ObjectModel;
using System.Windows.Controls;
using MoniHop.Core.Displays;
using MoniHop.Desktop.Models;

namespace MoniHop.Desktop.Views;

public partial class DisplaysPage : UserControl
{
    public DisplaysPage(IReadOnlyList<DisplaySnapshot> displays)
    {
        InitializeComponent();
        Displays = new ObservableCollection<DisplayViewModel>(DisplayViewModel.CreateAll(displays));
        DataContext = this;
    }

    public ObservableCollection<DisplayViewModel> Displays { get; }
}
