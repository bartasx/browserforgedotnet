namespace BrowserforgeDotnet.Headers;

/// <summary>
/// Utility functions for header processing
/// </summary>
public static class HeaderUtils
{
    /// <summary>
    /// Retrieves the User-Agent from the headers dictionary
    /// </summary>
    /// <param name="headers">Dictionary of HTTP headers</param>
    /// <returns>User-Agent string or null if not found</returns>
    public static string? GetUserAgent(Dictionary<string, string> headers)
    {
        if (headers == null)
            return null;

        return headers.TryGetValue("User-Agent", out var userAgent) ? userAgent :
               headers.TryGetValue("user-agent", out var userAgentLower) ? userAgentLower : null;
    }

    /// <summary>
    /// Determines the browser name from the User-Agent string
    /// </summary>
    /// <param name="userAgent">User-Agent string</param>
    /// <returns>Browser name or null if not recognized</returns>
    public static string? GetBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return null;

        // Edge detection (must come before Chrome since Edge contains "Chrome")
        if (userAgent.Contains("Edge") || userAgent.Contains("EdgA") ||
            userAgent.Contains("Edg/") || userAgent.Contains("EdgiOS"))
            return "edge";

        // Firefox detection
        if (userAgent.Contains("Firefox") || userAgent.Contains("FxiOS"))
            return "firefox";

        // Chrome detection
        if (userAgent.Contains("Chrome") || userAgent.Contains("CriOS"))
            return "chrome";

        // Safari detection
        if (userAgent.Contains("Safari"))
            return "safari";

        return null;
    }

    /// <summary>
    /// Converts a header name to Pascal case according to browser conventions
    /// </summary>
    /// <param name="name">Header name to pascalize</param>
    /// <returns>Pascalized header name</returns>
    public static string Pascalize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Ignore pseudo-headers and sec-ch-ua headers
        if (name.StartsWith(":") || name.StartsWith("sec-ch-ua"))
            return name;

        // Special uppercase headers
        if (HeaderConstants.PascalizeUpper.Contains(name.ToLowerInvariant()))
            return name.ToUpperInvariant();

        // Convert to title case
        return ToTitleCase(name);
    }

    /// <summary>
    /// Converts all header names in a dictionary to Pascal case
    /// </summary>
    /// <param name="headers">Dictionary of headers</param>
    /// <returns>New dictionary with pascalized header names</returns>
    public static Dictionary<string, string> PascalizeHeaders(Dictionary<string, string> headers)
    {
        if (headers == null)
            return new Dictionary<string, string>();

        return headers.ToDictionary(
            kvp => Pascalize(kvp.Key),
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts an object to a tuple-like collection if it's not already iterable
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    /// <param name="obj">Object to convert</param>
    /// <returns>Collection containing the object or the object itself if already a collection</returns>
    public static IEnumerable<T> Tuplify<T>(T? obj)
    {
        if (obj == null)
            return Enumerable.Empty<T>();

        if (obj is IEnumerable<T> enumerable && obj is not string)
            return enumerable;

        return new[] { obj };
    }

    /// <summary>
    /// Converts an object to a tuple-like collection of strings
    /// </summary>
    /// <param name="obj">Object to convert</param>
    /// <returns>Collection of strings</returns>
    public static IEnumerable<string> TuplifyStrings(object? obj)
    {
        if (obj == null)
            return Enumerable.Empty<string>();

        if (obj is string str)
            return new[] { str };

        if (obj is IEnumerable<string> strEnumerable)
            return strEnumerable;

        if (obj is System.Collections.IEnumerable enumerable)
            return enumerable.Cast<object>().Select(x => x?.ToString() ?? string.Empty);

        return new[] { obj.ToString() ?? string.Empty };
    }

    /// <summary>
    /// Converts string to title case (first letter of each word capitalized)
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Title case string</returns>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split('-');
        var titleCaseWords = words.Select(word =>
        {
            if (string.IsNullOrEmpty(word))
                return word;

            return char.ToUpperInvariant(word[0]) + 
                   (word.Length > 1 ? word.Substring(1).ToLowerInvariant() : string.Empty);
        });

        return string.Join("-", titleCaseWords);
    }

    /// <summary>
    /// Generates an Accept-Language header value from a collection of locales
    /// </summary>
    /// <param name="locales">Collection of locale strings</param>
    /// <returns>Accept-Language header value</returns>
    public static string GenerateAcceptLanguageHeader(IEnumerable<string> locales)
    {
        if (locales == null)
            return "en-US";

        var localeList = locales.ToList();
        if (localeList.Count == 0)
            return "en-US";

        var acceptLanguageItems = localeList.Select((locale, index) =>
        {
            var quality = 1.0 - (index * 0.1);
            return quality >= 1.0 ? locale : $"{locale};q={quality.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}";
        });

        return string.Join(", ", acceptLanguageItems);
    }

    /// <summary>
    /// Determines if Sec-Fetch headers should be added based on the browser
    /// </summary>
    /// <param name="browser">Browser object</param>
    /// <returns>True if Sec-Fetch headers should be added</returns>
    public static bool ShouldAddSecFetch(HttpBrowserObject browser)
    {
        if (browser?.Name == null || browser.Version.Length == 0)
            return false;

        return browser.Name switch
        {
            "chrome" => browser.Version[0] >= 76,
            "firefox" => browser.Version[0] >= 90,
            "edge" => browser.Version[0] >= 79,
            _ => false
        };
    }

    /// <summary>
    /// Filters browser HTTP values based on HTTP version constraints
    /// </summary>
    /// <param name="value">Browser HTTP value to filter</param>
    /// <param name="http1Values">HTTP/1 constraints</param>
    /// <param name="http2Values">HTTP/2 constraints</param>
    /// <returns>True if the value should be included</returns>
    public static bool FilterBrowserHttp(string value, 
        Dictionary<string, IEnumerable<string>>? http1Values,
        Dictionary<string, IEnumerable<string>>? http2Values)
    {
        var parts = value.Split('|');
        if (parts.Length != 2)
            return false;

        var browserName = parts[0];
        var httpVersion = parts[1];

        if (httpVersion == "1")
        {
            return http1Values == null || 
                   !http1Values.TryGetValue("*BROWSER", out var browsers) ||
                   browsers.Contains(browserName);
        }

        return http2Values == null || 
               !http2Values.TryGetValue("*BROWSER", out var browsers2) ||
               browsers2.Contains(browserName);
    }

    /// <summary>
    /// Filters attribute values based on HTTP version constraints
    /// </summary>
    /// <param name="value">Attribute value to filter</param>
    /// <param name="http1Values">HTTP/1 constraints</param>
    /// <param name="http2Values">HTTP/2 constraints</param>
    /// <param name="key">Attribute key</param>
    /// <returns>True if the value should be included</returns>
    public static bool FilterOtherValues(string value,
        Dictionary<string, IEnumerable<string>>? http1Values,
        Dictionary<string, IEnumerable<string>>? http2Values,
        string key)
    {
        if (http1Values == null && http2Values == null)
            return true;

        var inHttp1 = http1Values?.TryGetValue(key, out var http1Vals) == true && 
                      http1Vals.Contains(value);
        var inHttp2 = http2Values?.TryGetValue(key, out var http2Vals) == true && 
                      http2Vals.Contains(value);

        return inHttp1 || inHttp2;
    }
}