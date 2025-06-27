namespace BrowserforgeDotnet.Headers;

/// <summary>
/// Represents a browser specification with name, min/max version constraints, and HTTP version
/// </summary>
public record Browser
{
    /// <summary>
    /// Browser name (e.g., 'chrome', 'firefox', 'safari', 'edge')
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Minimum version constraint (optional)
    /// </summary>
    public int? MinVersion { get; init; }

    /// <summary>
    /// Maximum version constraint (optional)
    /// </summary>
    public int? MaxVersion { get; init; }

    /// <summary>
    /// HTTP version ('1' or '2')
    /// </summary>
    public string HttpVersion { get; init; } = "2";

    /// <summary>
    /// Initializes a new instance of the Browser record
    /// </summary>
    /// <param name="name">Browser name</param>
    /// <param name="minVersion">Minimum version constraint</param>
    /// <param name="maxVersion">Maximum version constraint</param>
    /// <param name="httpVersion">HTTP version</param>
    /// <exception cref="ArgumentException">Thrown when min version exceeds max version</exception>
    public Browser(string name, int? minVersion = null, int? maxVersion = null, string httpVersion = "2")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        MinVersion = minVersion;
        MaxVersion = maxVersion;
        HttpVersion = httpVersion?.ToString() ?? "2";

        // Validate version constraints
        if (MinVersion.HasValue && MaxVersion.HasValue && MinVersion > MaxVersion)
        {
            throw new ArgumentException(
                $"Browser min version constraint ({MinVersion}) cannot exceed max version ({MaxVersion})",
                nameof(minVersion));
        }
    }

    /// <summary>
    /// Creates a Browser instance from a name string
    /// </summary>
    /// <param name="name">Browser name</param>
    /// <param name="httpVersion">HTTP version</param>
    /// <returns>Browser instance</returns>
    public static Browser FromName(string name, string httpVersion = "2")
    {
        return new Browser(name, httpVersion: httpVersion);
    }
}