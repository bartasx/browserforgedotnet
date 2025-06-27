namespace BrowserforgeDotnet.Headers;

/// <summary>
/// Constants used in header generation
/// </summary>
public static class HeaderConstants
{
    /// <summary>
    /// Supported browser names
    /// </summary>
    public static readonly string[] SupportedBrowsers = { "chrome", "firefox", "safari", "edge" };

    /// <summary>
    /// Supported operating system names
    /// </summary>
    public static readonly string[] SupportedOperatingSystems = { "windows", "macos", "linux", "android", "ios" };

    /// <summary>
    /// Supported device types
    /// </summary>
    public static readonly string[] SupportedDevices = { "desktop", "mobile" };

    /// <summary>
    /// Supported HTTP versions
    /// </summary>
    public static readonly string[] SupportedHttpVersions = { "1", "2" };

    /// <summary>
    /// Token used to represent missing values in the dataset
    /// </summary>
    public const string MissingValueDatasetToken = "*MISSING_VALUE*";

    /// <summary>
    /// Sec-Fetch headers for HTTP/1.1
    /// </summary>
    public static readonly Dictionary<string, string> Http1SecFetchAttributes = new()
    {
        { "Sec-Fetch-Mode", "same-site" },
        { "Sec-Fetch-Dest", "navigate" },
        { "Sec-Fetch-Site", "?1" },
        { "Sec-Fetch-User", "document" }
    };

    /// <summary>
    /// Sec-Fetch headers for HTTP/2
    /// </summary>
    public static readonly Dictionary<string, string> Http2SecFetchAttributes = new()
    {
        { "sec-fetch-mode", "same-site" },
        { "sec-fetch-dest", "navigate" },
        { "sec-fetch-site", "?1" },
        { "sec-fetch-user", "document" }
    };

    /// <summary>
    /// Order in which constraints are relaxed when generation fails
    /// </summary>
    public static readonly string[] RelaxationOrder = { "locales", "devices", "operatingSystems", "browsers" };

    /// <summary>
    /// Headers that should be uppercased in pascalization
    /// </summary>
    public static readonly HashSet<string> PascalizeUpper = new() { "dnt", "rtt", "ect" };
}