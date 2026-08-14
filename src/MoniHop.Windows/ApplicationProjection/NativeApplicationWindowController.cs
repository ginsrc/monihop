using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.ApplicationProjection;

[SupportedOSPlatform("windows")]
public sealed class NativeApplicationWindowController : IApplicationWindowController
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const int ErrorInsufficientBuffer = 122;
    private const int AppModelErrorNoPackage = 15700;
    private const int SwRestore = 9;
    private const int SwMaximize = 3;

    private static readonly PropertyKey AppUserModelIdKey = new(
        new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        5);

    private readonly IWindowController _windowController;

    public NativeApplicationWindowController(IWindowController? windowController = null)
    {
        _windowController = windowController ?? new NativeWindowController();
    }

    public ApplicationWindowSnapshot? Read(nint windowHandle)
    {
        var placement = _windowController.ReadPlacement(windowHandle);
        if (placement is null || GetWindowThreadProcessId(windowHandle, out var processId) == 0)
        {
            return null;
        }

        if (processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        var executableIdentity = ReadExecutableIdentity(processId);
        var isApplicationFrameHost = executableIdentity is not null &&
            string.Equals(
                Path.GetFileName(executableIdentity.Value),
                "ApplicationFrameHost.exe",
                StringComparison.OrdinalIgnoreCase);
        var identity = isApplicationFrameHost
            ? ReadHostedApplicationAumid(windowHandle, processId)
            : ReadProcessAumid(processId) ?? executableIdentity;
        if (identity is null)
        {
            return null;
        }

        return new ApplicationWindowSnapshot(
            windowHandle,
            identity,
            ReadDisplayName(identity),
            placement.Value,
            _windowController.ReadCapabilities(windowHandle));
    }

    public IReadOnlyList<ApplicationWindowSnapshot> ReadAll()
    {
        var windows = new List<ApplicationWindowSnapshot>();
        _ = EnumWindows(
            (windowHandle, _) =>
            {
                try
                {
                    var snapshot = Read(windowHandle);
                    if (snapshot is not null)
                    {
                        windows.Add(snapshot);
                    }
                }
                catch (Win32Exception)
                {
                    // A process can disappear or reject access while windows are enumerated.
                }

                return true;
            },
            0);

        return windows
            .GroupBy(item => $"{item.Application.Kind}:{item.Application.Value}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public void Move(nint windowHandle, ApplicationProjectionPlan plan)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A native window handle is required.", nameof(windowHandle));
        }

        ArgumentNullException.ThrowIfNull(plan);
        var current = _windowController.ReadPlacement(windowHandle) ??
            throw new ArgumentException("The native window is no longer available.", nameof(windowHandle));

        if (current.IsMaximized &&
            plan.Layout is ProjectionLayout.LeftHalf or ProjectionLayout.RightHalf)
        {
            _ = ShowWindowAsync(windowHandle, SwRestore);
        }

        var showCommand = plan.Layout == ProjectionLayout.KeepSize
            ? current.ShowCommand
            : 1u;
        _windowController.MoveWindow(
            windowHandle,
            new WindowPlacementSnapshot(
                showCommand,
                plan.TargetRect,
                plan.TargetRect));

        if (plan.ShouldMaximize)
        {
            _ = ShowWindowAsync(windowHandle, SwMaximize);
        }
    }

    private static ApplicationIdentity? ReadWindowAumid(nint windowHandle)
    {
        IPropertyStore? store = null;
        try
        {
            var interfaceId = typeof(IPropertyStore).GUID;
            if (SHGetPropertyStoreForWindow(windowHandle, ref interfaceId, out store) != 0 || store is null)
            {
                return null;
            }

            var propertyKey = AppUserModelIdKey;
            if (store.GetValue(ref propertyKey, out var value) != 0)
            {
                return null;
            }

            try
            {
                var aumid = value.GetString();
                return string.IsNullOrWhiteSpace(aumid)
                    ? null
                    : new ApplicationIdentity(ApplicationIdentityKind.ApplicationUserModelId, aumid);
            }
            finally
            {
                _ = PropVariantClear(ref value);
            }
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            if (store is not null)
            {
                _ = Marshal.ReleaseComObject(store);
            }
        }
    }

    private static ApplicationIdentity? ReadProcessAumid(uint processId)
    {
        using var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process.IsInvalid)
        {
            ThrowIfAccessDenied();
            return null;
        }

        uint packageFamilyNameLength = 0;
        var packageResult = GetPackageFamilyName(
            process.DangerousGetHandle(),
            ref packageFamilyNameLength,
            null);
        if (packageResult == AppModelErrorNoPackage ||
            packageResult != ErrorInsufficientBuffer ||
            packageFamilyNameLength == 0)
        {
            return null;
        }

        uint length = 0;
        var result = GetApplicationUserModelId(process.DangerousGetHandle(), ref length, null);
        if (result != ErrorInsufficientBuffer || length == 0)
        {
            return null;
        }

        var value = new StringBuilder((int)length);
        result = GetApplicationUserModelId(process.DangerousGetHandle(), ref length, value);
        return result == 0 && value.Length > 0
            ? new ApplicationIdentity(ApplicationIdentityKind.ApplicationUserModelId, value.ToString())
            : null;
    }

    private static ApplicationIdentity? ReadHostedApplicationAumid(
        nint hostWindow,
        uint hostProcessId)
    {
        ApplicationIdentity? identity = null;
        _ = EnumChildWindows(
            hostWindow,
            (childWindow, _) =>
            {
                if (GetWindowThreadProcessId(childWindow, out var childProcessId) == 0 ||
                    childProcessId == hostProcessId)
                {
                    return true;
                }

                identity = ReadProcessAumid(childProcessId) ?? ReadWindowAumid(childWindow);
                return identity is null;
            },
            0);
        return identity;
    }

    private static ApplicationIdentity? ReadExecutableIdentity(uint processId)
    {
        using var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process.IsInvalid)
        {
            ThrowIfAccessDenied();
            return null;
        }

        var capacity = 32768;
        var path = new StringBuilder(capacity);
        return QueryFullProcessImageName(process, 0, path, ref capacity) && path.Length > 0
            ? new ApplicationIdentity(
                ApplicationIdentityKind.ExecutablePath,
                Path.GetFullPath(path.ToString()))
            : null;
    }

    private static void ThrowIfAccessDenied()
    {
        const int errorAccessDenied = 5;
        var error = Marshal.GetLastWin32Error();
        if (error == errorAccessDenied)
        {
            throw new Win32Exception(error);
        }
    }

    private static string ReadDisplayName(ApplicationIdentity identity)
    {
        if (identity.Kind == ApplicationIdentityKind.ApplicationUserModelId)
        {
            var separator = identity.Value.IndexOf('!');
            return separator > 0 ? identity.Value[..separator] : identity.Value;
        }

        try
        {
            var info = FileVersionInfo.GetVersionInfo(identity.Value);
            if (!string.IsNullOrWhiteSpace(info.ProductName))
            {
                return info.ProductName;
            }
        }
        catch (FileNotFoundException)
        {
        }

        return Path.GetFileNameWithoutExtension(identity.Value);
    }

    private delegate bool EnumWindowsProc(nint windowHandle, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(
        nint parentWindow,
        EnumWindowsProc callback,
        nint parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(nint windowHandle, int command);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        Microsoft.Win32.SafeHandles.SafeProcessHandle process,
        uint flags,
        StringBuilder executableName,
        ref int size);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(
        nint process,
        ref uint applicationUserModelIdLength,
        StringBuilder? applicationUserModelId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFamilyName(
        nint process,
        ref uint packageFamilyNameLength,
        StringBuilder? packageFamilyName);

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(
        nint windowHandle,
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore propertyStore);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        int GetCount(out uint propertyCount);
        int GetAt(uint propertyIndex, out PropertyKey key);
        int GetValue(ref PropertyKey key, out PropVariant value);
        int SetValue(ref PropertyKey key, ref PropVariant value);
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PropertyKey(Guid formatId, uint propertyId)
    {
        public Guid FormatId = formatId;
        public uint PropertyId = propertyId;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort VariantType;
        [FieldOffset(8)] public nint PointerValue;

        public readonly string? GetString() =>
            VariantType == 31 && PointerValue != 0
                ? Marshal.PtrToStringUni(PointerValue)
                : null;
    }
}
