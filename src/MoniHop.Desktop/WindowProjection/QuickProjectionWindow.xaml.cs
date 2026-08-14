using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;

namespace MoniHop.Desktop.WindowProjection;

public partial class QuickProjectionWindow : Window, INotifyPropertyChanged
{
    private QuickProjectionDisplayOption? _selectedDisplay;
    private QuickProjectionLayoutOption? _selectedLayout;

    public QuickProjectionWindow(
        WindowProjectionCandidate candidate,
        IReadOnlyList<DisplaySnapshot> displays,
        string? defaultTargetDisplayId,
        ProjectionLayout defaultLayout)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        ArgumentNullException.ThrowIfNull(displays);
        Displays = displays.Select((display, index) => new QuickProjectionDisplayOption(
            display.StableId,
            (index + 1).ToString(),
            display.DisplayName,
            $"{display.ResolutionWidth} × {display.ResolutionHeight}"))
            .ToArray();
        Layouts =
        [
            new(ProjectionLayout.KeepSize, "保持尺寸"),
            new(ProjectionLayout.Maximized, "最大化"),
            new(ProjectionLayout.LeftHalf, "左半屏"),
            new(ProjectionLayout.RightHalf, "右半屏"),
        ];
        _selectedDisplay = Displays.FirstOrDefault(item =>
            StringComparer.OrdinalIgnoreCase.Equals(item.StableId, defaultTargetDisplayId)) ?? Displays.FirstOrDefault();
        _selectedLayout = Layouts.First(item => item.Layout == defaultLayout);
        InitializeComponent();
        DataContext = this;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<QuickProjectionRequest>? ProjectionRequested;

    public WindowProjectionCandidate Candidate { get; }

    public IReadOnlyList<QuickProjectionDisplayOption> Displays { get; }

    public IReadOnlyList<QuickProjectionLayoutOption> Layouts { get; }

    public QuickProjectionDisplayOption? SelectedDisplay
    {
        get => _selectedDisplay;
        set => Set(ref _selectedDisplay, value);
    }

    public QuickProjectionLayoutOption? SelectedLayout
    {
        get => _selectedLayout;
        set => Set(ref _selectedLayout, value);
    }

    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorNotice.Visibility = Visibility.Visible;
    }

    private void ProjectButton_OnClick(object sender, RoutedEventArgs e) => RequestProjection();

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private void QuickProjectionWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            RequestProjection();
        }
    }

    private void RequestProjection()
    {
        if (SelectedDisplay is null || SelectedLayout is null)
        {
            ShowError("请选择目标显示器和窗口布局。");
            return;
        }

        ProjectionRequested?.Invoke(this, new QuickProjectionRequest(
            SelectedDisplay.StableId,
            SelectedLayout.Layout));
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record QuickProjectionDisplayOption(
    string StableId,
    string Number,
    string Name,
    string Resolution);

public sealed record QuickProjectionLayoutOption(ProjectionLayout Layout, string Name);

public sealed record QuickProjectionRequest(string TargetDisplayId, ProjectionLayout Layout);
