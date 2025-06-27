namespace BrowserforgeDotnet.Headers;

/// <summary>
/// Configuration options for the HeaderGenerator
/// </summary>
public record HeaderGeneratorOptions
{
    /// <summary>
    /// Supported browsers or Browser objects
    /// </summary>
    public IEnumerable<object> Browsers { get; init; } = HeaderConstants.SupportedBrowsers;

    /// <summary>
    /// Supported operating systems
    /// </summary>
    public IEnumerable<string> OperatingSystems { get; init; } = HeaderConstants.SupportedOperatingSystems;

    /// <summary>
    /// Supported devices
    /// </summary>
    public IEnumerable<string> Devices { get; init; } = HeaderConstants.SupportedDevices;

    /// <summary>
    /// Supported locales for Accept-Language header
    /// </summary>
    public IEnumerable<string> Locales { get; init; } = new[] { "en-US" };

    /// <summary>
    /// HTTP version to use ('1' or '2')
    /// </summary>
    public string HttpVersion { get; init; } = "2";

    /// <summary>
    /// Whether to throw an error if headers cannot be generated
    /// </summary>
    public bool Strict { get; init; } = false;
}

/// <summary>
/// Request parameters for header generation that can override default options
/// </summary>
public record HeaderGenerationRequest
{
    /// <summary>
    /// Browsers to generate headers for
    /// </summary>
    public IEnumerable<object>? Browsers { get; init; }

    /// <summary>
    /// Operating systems to generate headers for
    /// </summary>
    public IEnumerable<string>? OperatingSystems { get; init; }

    /// <summary>
    /// Devices to generate headers for
    /// </summary>
    public IEnumerable<string>? Devices { get; init; }

    /// <summary>
    /// Locales for Accept-Language header
    /// </summary>
    public IEnumerable<string>? Locales { get; init; }

    /// <summary>
    /// HTTP version to use
    /// </summary>
    public string? HttpVersion { get; init; }

    /// <summary>
    /// Whether to throw an error if headers cannot be generated
    /// </summary>
    public bool? Strict { get; init; }

    /// <summary>
    /// Specific User-Agent values to use
    /// </summary>
    public IEnumerable<string>? UserAgent { get; init; }

    /// <summary>
    /// Known values of request-dependent headers
    /// </summary>
    public Dictionary<string, string>? RequestDependentHeaders { get; init; }
}

/// <summary>
/// Effective options used internally for header generation
/// </summary>
internal record EffectiveHeaderOptions(
    IEnumerable<Browser> Browsers,
    IEnumerable<string> OperatingSystems,
    IEnumerable<string> Devices,
    IEnumerable<string> Locales,
    string HttpVersion,
    bool Strict,
    IEnumerable<string>? UserAgent = null,
    Dictionary<string, string>? RequestDependentHeaders = null
);

/// <summary>
/// Builder pattern for creating HeaderGenerationRequest instances
/// </summary>
public class HeaderGenerationRequestBuilder
{
    private readonly List<object> _browsers = new();
    private readonly List<string> _operatingSystems = new();
    private readonly List<string> _devices = new();
    private readonly List<string> _locales = new();
    private readonly List<string> _userAgents = new();
    private readonly Dictionary<string, string> _requestDependentHeaders = new();
    private string? _httpVersion;
    private bool? _strict;

    /// <summary>
    /// Adds a browser specification
    /// </summary>
    /// <param name="browser">Browser name or Browser object</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithBrowser(object browser)
    {
        _browsers.Add(browser);
        return this;
    }

    /// <summary>
    /// Adds multiple browser specifications
    /// </summary>
    /// <param name="browsers">Browser names or Browser objects</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithBrowsers(params object[] browsers)
    {
        _browsers.AddRange(browsers);
        return this;
    }

    /// <summary>
    /// Adds an operating system
    /// </summary>
    /// <param name="os">Operating system name</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithOperatingSystem(string os)
    {
        _operatingSystems.Add(os);
        return this;
    }

    /// <summary>
    /// Adds multiple operating systems
    /// </summary>
    /// <param name="operatingSystems">Operating system names</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithOperatingSystems(params string[] operatingSystems)
    {
        _operatingSystems.AddRange(operatingSystems);
        return this;
    }

    /// <summary>
    /// Adds a device type
    /// </summary>
    /// <param name="device">Device type</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithDevice(string device)
    {
        _devices.Add(device);
        return this;
    }

    /// <summary>
    /// Adds multiple device types
    /// </summary>
    /// <param name="devices">Device types</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithDevices(params string[] devices)
    {
        _devices.AddRange(devices);
        return this;
    }

    /// <summary>
    /// Adds a locale
    /// </summary>
    /// <param name="locale">Locale string</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithLocale(string locale)
    {
        _locales.Add(locale);
        return this;
    }

    /// <summary>
    /// Adds multiple locales
    /// </summary>
    /// <param name="locales">Locale strings</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithLocales(params string[] locales)
    {
        _locales.AddRange(locales);
        return this;
    }

    /// <summary>
    /// Sets the HTTP version
    /// </summary>
    /// <param name="httpVersion">HTTP version ('1' or '2')</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithHttpVersion(string httpVersion)
    {
        _httpVersion = httpVersion;
        return this;
    }

    /// <summary>
    /// Sets the strict mode
    /// </summary>
    /// <param name="strict">Whether to use strict mode</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithStrict(bool strict)
    {
        _strict = strict;
        return this;
    }

    /// <summary>
    /// Adds a User-Agent value
    /// </summary>
    /// <param name="userAgent">User-Agent string</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithUserAgent(string userAgent)
    {
        _userAgents.Add(userAgent);
        return this;
    }

    /// <summary>
    /// Adds multiple User-Agent values
    /// </summary>
    /// <param name="userAgents">User-Agent strings</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithUserAgents(params string[] userAgents)
    {
        _userAgents.AddRange(userAgents);
        return this;
    }

    /// <summary>
    /// Adds a request-dependent header
    /// </summary>
    /// <param name="name">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithRequestDependentHeader(string name, string value)
    {
        _requestDependentHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple request-dependent headers
    /// </summary>
    /// <param name="headers">Dictionary of headers</param>
    /// <returns>Builder instance for chaining</returns>
    public HeaderGenerationRequestBuilder WithRequestDependentHeaders(Dictionary<string, string> headers)
    {
        foreach (var kvp in headers)
        {
            _requestDependentHeaders[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Builds the HeaderGenerationRequest
    /// </summary>
    /// <returns>HeaderGenerationRequest instance</returns>
    public HeaderGenerationRequest Build()
    {
        return new HeaderGenerationRequest
        {
            Browsers = _browsers.Any() ? _browsers : null,
            OperatingSystems = _operatingSystems.Any() ? _operatingSystems : null,
            Devices = _devices.Any() ? _devices : null,
            Locales = _locales.Any() ? _locales : null,
            HttpVersion = _httpVersion,
            Strict = _strict,
            UserAgent = _userAgents.Any() ? _userAgents : null,
            RequestDependentHeaders = _requestDependentHeaders.Any() ? _requestDependentHeaders : null
        };
    }
}