using MoniHop.Desktop.Localization;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace MoniHop.Desktop.Lifecycle;

public sealed class TrayIconService : IDisposable
{
    private readonly LocalizationService _localization;
    private readonly Drawing.Icon _icon;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _openItem;
    private readonly Forms.ToolStripMenuItem _toggleProjectionItem;
    private readonly Forms.ToolStripMenuItem _recallItem;
    private readonly Forms.ToolStripMenuItem _exitItem;
    private bool _applicationProjectionEnabled;
    private bool _disposed;

    public TrayIconService(LocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        var menu = new Forms.ContextMenuStrip();
        _openItem = new Forms.ToolStripMenuItem();
        _toggleProjectionItem = new Forms.ToolStripMenuItem();
        _recallItem = new Forms.ToolStripMenuItem();
        _exitItem = new Forms.ToolStripMenuItem();
        menu.Items.AddRange([
            _openItem,
            new Forms.ToolStripSeparator(),
            _toggleProjectionItem,
            _recallItem,
            new Forms.ToolStripSeparator(),
            _exitItem,
        ]);
        _openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _toggleProjectionItem.Click += (_, _) => ToggleApplicationProjectionRequested?.Invoke(this, EventArgs.Empty);
        _recallItem.Click += (_, _) => RecallRequested?.Invoke(this, EventArgs.Empty);
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        _icon = ReadApplicationIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = _icon,
            Visible = true,
        };
        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
            }
        };
        RefreshText();
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? ToggleApplicationProjectionRequested;

    public event EventHandler? RecallRequested;

    public event EventHandler? ExitRequested;

    public void SetApplicationProjectionEnabled(bool enabled)
    {
        _applicationProjectionEnabled = enabled;
        RefreshText();
    }

    public void RefreshText()
    {
        _notifyIcon.Text = _localization.Get("String.AppName");
        _openItem.Text = _localization.Get("String.Tray.Open");
        _toggleProjectionItem.Text = _localization.Get(
            _applicationProjectionEnabled ? "String.Tray.PauseProjection" : "String.Tray.ResumeProjection");
        _recallItem.Text = _localization.Get("String.Tray.Recall");
        _exitItem.Text = _localization.Get("String.Tray.Exit");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Drawing.Icon ReadApplicationIcon()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            using var extracted = Drawing.Icon.ExtractAssociatedIcon(path);
            if (extracted is not null)
            {
                return (Drawing.Icon)extracted.Clone();
            }
        }

        return (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
    }
}
