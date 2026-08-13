using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Models;

public sealed record DisplayViewModel(
    string ScreenLabel,
    string Name,
    string Resolution,
    string PrimaryStatus,
    string DeviceName,
    string StableId = "",
    string ConnectionStatus = "已连接",
    bool IsConnected = true,
    string RefreshRate = "未知",
    string Scale = "未知",
    string Orientation = "未知",
    string PhysicalSize = "未知",
    string PhysicalSizeDetail = "未知")
{
    public static IReadOnlyList<DisplayViewModel> CreateAll(
        IReadOnlyList<DisplaySnapshot> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);

        return displays
            .Select((display, index) => new DisplayViewModel(
                $"屏幕 {index + 1}",
                display.DisplayName,
                $"{display.ResolutionWidth} × {display.ResolutionHeight}",
                display.IsPrimary ? "Windows 主显示器" : "普通显示器",
                display.DeviceName,
                display.StableId,
                RefreshRate: FormatRefreshRate(display.RefreshRateHz),
                Scale: FormatScale(display.ScalePercent),
                Orientation: FormatOrientation(display.Orientation),
                PhysicalSize: FormatPhysicalSize(
                    display.PhysicalWidthMillimeters,
                    display.PhysicalHeightMillimeters),
                PhysicalSizeDetail: FormatPhysicalSizeDetail(
                    display.PhysicalWidthMillimeters,
                    display.PhysicalHeightMillimeters)))
            .ToArray();
    }

    public static DisplayViewModel Create(DisplayProfileState state, int connectedIndex)
    {
        ArgumentNullException.ThrowIfNull(state);
        var display = state.CurrentDisplay;
        return new DisplayViewModel(
            display is null ? "历史设备" : $"屏幕 {connectedIndex}",
            state.DisplayName,
            display is null
                ? state.Profile.LastResolution
                : $"{display.ResolutionWidth} × {display.ResolutionHeight}",
            display?.IsPrimary == true ? "Windows 主显示器" : "普通显示器",
            display?.DeviceName ?? string.Empty,
            state.Profile.StableId,
            state.ConnectionStatus,
            state.IsConnected,
            FormatRefreshRate(display?.RefreshRateHz ?? state.Profile.LastRefreshRateHz),
            FormatScale(display?.ScalePercent ?? state.Profile.LastScalePercent),
            FormatOrientation(display?.Orientation ?? state.Profile.LastOrientation),
            FormatPhysicalSize(
                display?.PhysicalWidthMillimeters ?? state.Profile.LastPhysicalWidthMillimeters,
                display?.PhysicalHeightMillimeters ?? state.Profile.LastPhysicalHeightMillimeters),
            FormatPhysicalSizeDetail(
                display?.PhysicalWidthMillimeters ?? state.Profile.LastPhysicalWidthMillimeters,
                display?.PhysicalHeightMillimeters ?? state.Profile.LastPhysicalHeightMillimeters));
    }

    public string OperationHint => IsConnected ? "连接中" : "可忘记";

    private static string FormatRefreshRate(int? value) => value is > 0 ? $"{value} Hz" : "未知";

    private static string FormatScale(int? value) => value is > 0 ? $"{value}%" : "未知";

    private static string FormatOrientation(DisplayOrientation value) => value switch
    {
        DisplayOrientation.Landscape => "横向",
        DisplayOrientation.Portrait => "纵向",
        DisplayOrientation.LandscapeFlipped => "横向翻转",
        DisplayOrientation.PortraitFlipped => "纵向翻转",
        _ => "未知",
    };

    private static string FormatPhysicalSize(int? widthMillimeters, int? heightMillimeters)
    {
        if (widthMillimeters is not > 0 || heightMillimeters is not > 0)
        {
            return "未知";
        }

        var diagonalInches = Math.Sqrt(
                Math.Pow(widthMillimeters.Value, 2) + Math.Pow(heightMillimeters.Value, 2)) /
            25.4;
        return $"{diagonalInches:0.0} 英寸";
    }

    private static string FormatPhysicalSizeDetail(int? widthMillimeters, int? heightMillimeters) =>
        widthMillimeters is > 0 && heightMillimeters is > 0
            ? $"{widthMillimeters} × {heightMillimeters} mm"
            : "未知";
}
