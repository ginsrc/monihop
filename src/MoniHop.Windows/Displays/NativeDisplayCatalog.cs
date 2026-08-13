using System.ComponentModel;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using MoniHop.Core.Displays;

namespace MoniHop.Windows.Displays;

[SupportedOSPlatform("windows")]
public sealed class NativeDisplayCatalog : IDisplayCatalog
{
    private const uint MonitorInfoPrimary = 0x00000001;
    private const int EnumCurrentSettings = -1;
    private const int EffectiveDpi = 0;

    public IReadOnlyList<DisplaySnapshot> ReadAll()
    {
        var displays = new List<DisplaySnapshot>();

        var succeeded = EnumDisplayMonitors(
            0,
            0,
            (monitor, _, _, _) =>
            {
                var info = MonitorInfoEx.Create();
                if (!GetMonitorInfo(monitor, ref info))
                {
                    return false;
                }

                var ordinal = displays.Count + 1;
                var metrics = ReadMetrics(monitor, info.DeviceName);
                displays.Add(new DisplaySnapshot(
                    info.DeviceName,
                    $"Display {ordinal}",
                    info.Monitor.ToPixelRect(),
                    info.WorkArea.ToPixelRect(),
                    (info.Flags & MonitorInfoPrimary) != 0,
                    ReadStableId(info.DeviceName),
                    metrics.RefreshRateHz,
                    metrics.ScalePercent,
                    metrics.Orientation,
                    metrics.PhysicalWidthMillimeters,
                    metrics.PhysicalHeightMillimeters,
                    metrics.ResolutionWidth,
                    metrics.ResolutionHeight));

                return true;
            },
            0);

        if (!succeeded)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return displays.AsReadOnly();
    }

    private delegate bool MonitorEnumProc(
        nint monitor,
        nint deviceContext,
        nint monitorRect,
        nint data);

    private static string ReadStableId(string deviceName)
    {
        var monitor = DisplayDevice.Create();
        return EnumDisplayDevices(deviceName, 0, ref monitor, 0) &&
               !string.IsNullOrWhiteSpace(monitor.DeviceId)
            ? monitor.DeviceId
            : deviceName;
    }

    [SupportedOSPlatform("windows")]
    private static DisplayMetrics ReadMetrics(nint monitor, string deviceName)
    {
        var mode = DeviceMode.Create();
        int? refreshRate = null;
        int? resolutionWidth = null;
        int? resolutionHeight = null;
        var orientation = DisplayOrientation.Unknown;
        if (EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode))
        {
            refreshRate = mode.DisplayFrequency > 0 ? (int)mode.DisplayFrequency : null;
            resolutionWidth = mode.PelsWidth > 0 ? (int)mode.PelsWidth : null;
            resolutionHeight = mode.PelsHeight > 0 ? (int)mode.PelsHeight : null;
            orientation = mode.DisplayOrientation switch
            {
                0 => DisplayOrientation.Landscape,
                1 => DisplayOrientation.Portrait,
                2 => DisplayOrientation.LandscapeFlipped,
                3 => DisplayOrientation.PortraitFlipped,
                _ => DisplayOrientation.Unknown,
            };
        }

        int? scalePercent = null;
        if (GetDpiForMonitor(monitor, EffectiveDpi, out var dpiX, out _) == 0 && dpiX > 0)
        {
            scalePercent = (int)Math.Round(dpiX * 100d / 96d);
        }

        var physicalSize = ReadEdidPhysicalSize(deviceName);

        return new DisplayMetrics(
            refreshRate,
            scalePercent,
            orientation,
            physicalSize?.WidthMillimeters,
            physicalSize?.HeightMillimeters,
            resolutionWidth,
            resolutionHeight);
    }

    [SupportedOSPlatform("windows")]
    private static (int WidthMillimeters, int HeightMillimeters)? ReadEdidPhysicalSize(string deviceName)
    {
        var displayDevice = DisplayDevice.Create();
        if (!EnumDisplayDevices(deviceName, 0, ref displayDevice, 0) ||
            string.IsNullOrWhiteSpace(displayDevice.DeviceId))
        {
            return null;
        }

        var parts = displayDevice.DeviceId.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            using var displayRoot = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Enum\DISPLAY\" + parts[1]);
            if (displayRoot is null)
            {
                return null;
            }

            if (parts.Length >= 3)
            {
                using var exactInstance = displayRoot.OpenSubKey(parts[2]);
                var exactEdid = ReadEdidValue(exactInstance);
                if (exactEdid is not null)
                {
                    return EdidPhysicalSizeParser.Parse(exactEdid);
                }
            }

            foreach (var instanceName in displayRoot.GetSubKeyNames())
            {
                using var instance = displayRoot.OpenSubKey(instanceName);
                var edid = ReadEdidValue(instance);
                var size = edid is null ? null : EdidPhysicalSizeParser.Parse(edid);
                if (size is not null)
                {
                    return size;
                }
            }
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static byte[]? ReadEdidValue(RegistryKey? instance)
    {
        using var parameters = instance?.OpenSubKey("Device Parameters");
        return parameters?.GetValue("EDID") as byte[];
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRect,
        MonitorEnumProc callback,
        nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx monitorInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(
        string deviceName,
        int modeNumber,
        ref DeviceMode deviceMode);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        nint monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly PixelRect ToPixelRect() => new(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public static MonitorInfoEx Create() => new()
        {
            Size = Marshal.SizeOf<MonitorInfoEx>(),
            DeviceName = string.Empty,
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string? deviceName,
        uint deviceIndex,
        ref DisplayDevice displayDevice,
        uint flags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

        public static DisplayDevice Create() => new()
        {
            Size = Marshal.SizeOf<DisplayDevice>(),
            DeviceName = string.Empty,
            DeviceString = string.Empty,
            DeviceId = string.Empty,
            DeviceKey = string.Empty,
        };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DeviceMode
    {
        private const int DeviceNameSize = 32;
        private const int FormNameSize = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DeviceNameSize)]
        public string DeviceName;

        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public int PositionX;
        public int PositionY;
        public uint DisplayOrientation;
        public uint DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TrueTypeOption;
        public short Collate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FormNameSize)]
        public string FormName;

        public ushort LogPixels;
        public uint BitsPerPixel;
        public uint PelsWidth;
        public uint PelsHeight;
        public uint DisplayFlags;
        public uint DisplayFrequency;
        public uint IcmMethod;
        public uint IcmIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;

        public static DeviceMode Create() => new()
        {
            DeviceName = string.Empty,
            FormName = string.Empty,
            Size = (ushort)Marshal.SizeOf<DeviceMode>(),
        };
    }

    private sealed record DisplayMetrics(
        int? RefreshRateHz,
        int? ScalePercent,
        DisplayOrientation Orientation,
        int? PhysicalWidthMillimeters,
        int? PhysicalHeightMillimeters,
        int? ResolutionWidth,
        int? ResolutionHeight);
}

public static class EdidPhysicalSizeParser
{
    public static (int WidthMillimeters, int HeightMillimeters)? Parse(byte[]? edid)
    {
        if (edid is null || edid.Length <= 22 || edid[21] == 0 || edid[22] == 0)
        {
            return null;
        }

        return (edid[21] * 10, edid[22] * 10);
    }
}
