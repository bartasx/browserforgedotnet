namespace BrowserforgeDotnet.Headers;

/// <summary>
/// Represents an HTTP browser object with parsed browser information
/// </summary>
public record HttpBrowserObject
{
    /// <summary>
    /// Browser name (e.g., 'chrome', 'firefox', 'safari', 'edge')
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Browser version as a tuple of integers (major, minor, patch, etc.)
    /// </summary>
    public int[] Version { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Complete browser string representation
    /// </summary>
    public string CompleteString { get; init; } = string.Empty;

    /// <summary>
    /// HTTP version ('1' or '2')
    /// </summary>
    public string HttpVersion { get; init; } = "2";

    /// <summary>
    /// Gets whether this browser object represents HTTP/2
    /// </summary>
    public bool IsHttp2 => HttpVersion == "2";

    /// <summary>
    /// Initializes a new instance of the HttpBrowserObject record
    /// </summary>
    /// <param name="name">Browser name</param>
    /// <param name="version">Browser version parts</param>
    /// <param name="completeString">Complete browser string</param>
    /// <param name="httpVersion">HTTP version</param>
    public HttpBrowserObject(string? name, int[] version, string completeString, string httpVersion)
    {
        Name = name;
        Version = version ?? Array.Empty<int>();
        CompleteString = completeString ?? string.Empty;
        HttpVersion = httpVersion ?? "2";
    }

    /// <summary>
    /// Creates an HttpBrowserObject from a browser HTTP string
    /// </summary>
    /// <param name="httpBrowserString">Browser HTTP string in format "browserName/version|httpVersion"</param>
    /// <returns>HttpBrowserObject instance</returns>
    /// <exception cref="ArgumentException">Thrown when the browser string format is invalid</exception>
    public static HttpBrowserObject FromString(string httpBrowserString)
    {
        if (string.IsNullOrEmpty(httpBrowserString))
        {
            throw new ArgumentException("Browser string cannot be null or empty", nameof(httpBrowserString));
        }

        var parts = httpBrowserString.Split('|');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid browser string format. Expected 'browser/version|httpVersion'", 
                nameof(httpBrowserString));
        }

        var browserString = parts[0];
        var httpVersion = parts[1];

        if (browserString == HeaderConstants.MissingValueDatasetToken)
        {
            return new HttpBrowserObject(
                name: null,
                version: Array.Empty<int>(),
                completeString: HeaderConstants.MissingValueDatasetToken,
                httpVersion: string.Empty);
        }

        var browserParts = browserString.Split('/');
        if (browserParts.Length != 2)
        {
            throw new ArgumentException("Invalid browser format. Expected 'browserName/version'", 
                nameof(httpBrowserString));
        }

        var browserName = browserParts[0];
        var versionString = browserParts[1];

        var versionParts = versionString.Split('.')
            .Select(part => int.TryParse(part, out var num) ? num : 0)
            .ToArray();

        return new HttpBrowserObject(
            name: browserName,
            version: versionParts,
            completeString: httpBrowserString,
            httpVersion: httpVersion);
    }

    /// <summary>
    /// Gets the major version number
    /// </summary>
    public int MajorVersion => Version.Length > 0 ? Version[0] : 0;

    /// <summary>
    /// Gets the minor version number
    /// </summary>
    public int MinorVersion => Version.Length > 1 ? Version[1] : 0;

    /// <summary>
    /// Gets the patch version number
    /// </summary>
    public int PatchVersion => Version.Length > 2 ? Version[2] : 0;
}