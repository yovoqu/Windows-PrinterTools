namespace WindowsPrinter.Infrastructure;

/// <summary>
/// Window breakpoints aligned with Microsoft responsive design guidance.
/// See https://learn.microsoft.com/en-us/windows/apps/design/layout/screen-sizes-and-breakpoints-for-responsive-design
/// </summary>
public static class ResponsiveBreakpoints
{
    /// <summary>Small: up to 640 epx — stack chrome, compact margins.</summary>
    public const double SmallMax = 640;

    /// <summary>Medium: 641–1007 epx — single-pane or tall two-pane.</summary>
    public const double MediumMin = 641;
    public const double MediumMax = 1007;

    /// <summary>Large: 1008+ epx — side-by-side workspace.</summary>
    public const double LargeMin = 1008;

    /// <summary>Extra large: roomy margins and inline action bar.</summary>
    public const double ExtraLargeMin = 1280;

    /// <summary>Minimum window width that fits queue + settings side-by-side.</summary>
    public const int WorkspaceMinWidth = 1008;

    public const int WorkspaceMinHeight = 640;
    public const int PreferredDefaultWidth = 1280;
    public const int PreferredDefaultHeight = 800;
}
