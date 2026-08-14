using System.ComponentModel;
using System.Runtime.CompilerServices;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Models;

public sealed class HotKeyStatusViewModel : INotifyPropertyChanged
{
    private bool _isAvailable = true;
    private string _status = "正在注册";
    private string _shortcut;

    public HotKeyStatusViewModel(string actionName, string shortcut)
    {
        ActionName = actionName;
        _shortcut = shortcut;
        Category = string.Empty;
        Description = string.Empty;
        CanEdit = false;
    }

    public HotKeyStatusViewModel(HotKeyActionDefinition definition, HotKeyGesture? gesture)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        ActionName = definition.ActionName;
        Category = definition.Category;
        Description = definition.Description;
        CanEdit = true;
        Gesture = gesture;
        _shortcut = HotKeyGestureFormatter.Format(gesture);

        if (gesture is null)
        {
            _isAvailable = false;
            _status = "未设置";
        }
        else if (!definition.IsTargetAvailable)
        {
            _isAvailable = false;
            _status = "目标未连接";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ActionName { get; }

    public string Category { get; }

    public string Description { get; }

    public bool CanEdit { get; }

    public HotKeyActionDefinition? Definition { get; }

    public HotKeyGesture? Gesture { get; private set; }

    public string Shortcut
    {
        get => _shortcut;
        private set => SetField(ref _shortcut, value);
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        private set => SetField(ref _isAvailable, value);
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public void Update(bool isAvailable)
    {
        if (Definition is { IsTargetAvailable: false })
        {
            IsAvailable = false;
            Status = "目标未连接";
            return;
        }

        IsAvailable = isAvailable;
        Status = isAvailable ? "可用" : "冲突";
    }

    public void UpdateStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        Status = status;
    }

    public void SetGesture(HotKeyGesture? gesture)
    {
        Gesture = gesture;
        Shortcut = HotKeyGestureFormatter.Format(gesture);
        if (Definition is { IsTargetAvailable: false } && gesture is not null)
        {
            IsAvailable = false;
            Status = "目标未连接";
            return;
        }

        IsAvailable = gesture is not null;
        Status = gesture is null ? "未设置" : "正在注册";
    }

    public void BeginCapture()
    {
        if (!CanEdit)
        {
            return;
        }

        Shortcut = "请按下组合键";
        Status = "录入中";
    }

    public void CancelCapture() => SetGesture(Gesture);

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
