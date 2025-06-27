namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Represents screen dimension constraints for fingerprint generation
/// </summary>
public record Screen
{
    /// <summary>
    /// Minimum screen width constraint
    /// </summary>
    public int? MinWidth { get; init; }

    /// <summary>
    /// Maximum screen width constraint
    /// </summary>
    public int? MaxWidth { get; init; }

    /// <summary>
    /// Minimum screen height constraint
    /// </summary>
    public int? MinHeight { get; init; }

    /// <summary>
    /// Maximum screen height constraint
    /// </summary>
    public int? MaxHeight { get; init; }

    /// <summary>
    /// Initializes a new Screen with optional dimension constraints
    /// </summary>
    /// <param name="minWidth">Minimum screen width</param>
    /// <param name="maxWidth">Maximum screen width</param>
    /// <param name="minHeight">Minimum screen height</param>
    /// <param name="maxHeight">Maximum screen height</param>
    public Screen(int? minWidth = null, int? maxWidth = null, int? minHeight = null, int? maxHeight = null)
    {
        // Validate constraints
        if (minWidth.HasValue && maxWidth.HasValue && minWidth > maxWidth)
        {
            throw new ArgumentException("Minimum width cannot be greater than maximum width", nameof(minWidth));
        }

        if (minHeight.HasValue && maxHeight.HasValue && minHeight > maxHeight)
        {
            throw new ArgumentException("Minimum height cannot be greater than maximum height", nameof(minHeight));
        }

        MinWidth = minWidth;
        MaxWidth = maxWidth;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }

    /// <summary>
    /// Determines if any constraints are set
    /// </summary>
    /// <returns>True if any constraint values are specified</returns>
    public bool IsSet()
    {
        return MinWidth.HasValue || MaxWidth.HasValue || MinHeight.HasValue || MaxHeight.HasValue;
    }

    /// <summary>
    /// Checks if the given screen dimensions satisfy these constraints
    /// </summary>
    /// <param name="width">Screen width to check</param>
    /// <param name="height">Screen height to check</param>
    /// <returns>True if dimensions satisfy all constraints</returns>
    public bool SatisfiesConstraints(int width, int height)
    {
        if (MinWidth.HasValue && width < MinWidth.Value)
            return false;

        if (MaxWidth.HasValue && width > MaxWidth.Value)
            return false;

        if (MinHeight.HasValue && height < MinHeight.Value)
            return false;

        if (MaxHeight.HasValue && height > MaxHeight.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a Screen constraint for common desktop resolutions
    /// </summary>
    /// <returns>Screen constraint for desktop resolutions</returns>
    public static Screen Desktop()
    {
        return new Screen(minWidth: 1024, minHeight: 768);
    }

    /// <summary>
    /// Creates a Screen constraint for mobile resolutions
    /// </summary>
    /// <returns>Screen constraint for mobile resolutions</returns>
    public static Screen Mobile()
    {
        return new Screen(maxWidth: 768, maxHeight: 1024);
    }

    /// <summary>
    /// Creates a Screen constraint for tablet resolutions
    /// </summary>
    /// <returns>Screen constraint for tablet resolutions</returns>
    public static Screen Tablet()
    {
        return new Screen(minWidth: 768, maxWidth: 1366, minHeight: 1024, maxHeight: 1366);
    }
}