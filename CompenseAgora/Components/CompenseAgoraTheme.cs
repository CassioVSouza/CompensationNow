using MudBlazor;

namespace CompenseAgora.Components;

/// <summary>
/// Custom MudBlazor theme tokens for CompenseAgora, tuned to sit alongside the
/// existing Bootstrap-based pages rather than clash with them: the primary hue
/// matches the current sidebar/link blue, and typography reuses the app's
/// existing system sans stack instead of MudBlazor's default Roboto.
/// </summary>
public static class CompenseAgoraTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1b6ec2",
            Secondary = "#3a0647",
            AppbarBackground = "#052767",
            Background = "#f9f9f7",
            Surface = "#ffffff",
            TextPrimary = "#0b0b0b",
            TextSecondary = "#52514e",
            Success = "#0ca30c",
            Warning = "#eda100",
            Error = "#d03b3b",
            Info = "#2a78d6",
            LinesDefault = "#e1e0d9",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#3987e5",
            Secondary = "#9085e9",
            Background = "#0d0d0d",
            Surface = "#1a1a19",
            TextPrimary = "#ffffff",
            TextSecondary = "#c3c2b7",
            Success = "#0ca30c",
            Warning = "#c98500",
            Error = "#e66767",
            Info = "#3987e5",
            LinesDefault = "#2c2c2a",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"],
            },
            H1 = new H1Typography { FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"], FontWeight = "600" },
            H2 = new H2Typography { FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"], FontWeight = "600" },
            H3 = new H3Typography { FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"], FontWeight = "600" },
            H4 = new H4Typography { FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"], FontWeight = "600" },
            H5 = new H5Typography { FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"], FontWeight = "600" },
            H6 = new H6Typography { FontFamily = ["Helvetica Neue", "Helvetica", "Arial", "sans-serif"], FontWeight = "600" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
        },
    };

    /// <summary>
    /// The two-series emission chart palette (trips, energy), validated for
    /// categorical CVD-safety against the reference dataviz palette.
    /// </summary>
    public static readonly string[] ChartSeriesColors = ["#2a78d6", "#eb6834"];

    /// <summary>Single-hue accent used for the total-emissions trend line.</summary>
    public const string TrendColor = "#2a78d6";
}
