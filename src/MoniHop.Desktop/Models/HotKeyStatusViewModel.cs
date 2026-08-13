using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MoniHop.Desktop.Models;

public sealed class HotKeyStatusViewModel : INotifyPropertyChanged
{
    private bool _isAvailable = true;
    private string _status = "正在注册";

    public HotKeyStatusViewModel(string actionName, string shortcut)
    {
        ActionName = actionName;
        Shortcut = shortcut;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ActionName { get; }

    public string Shortcut { get; }

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
        IsAvailable = isAvailable;
        Status = isAvailable ? "可用" : "冲突";
    }

    public void UpdateStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        Status = status;
    }

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
