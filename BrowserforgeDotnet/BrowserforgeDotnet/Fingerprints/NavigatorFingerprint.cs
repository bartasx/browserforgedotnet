using System.Text.Json.Serialization;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Represents browser navigator properties for fingerprinting
/// </summary>
/// <param name="UserAgent">Browser user agent string</param>
/// <param name="UserAgentData">Structured user agent data</param>
/// <param name="DoNotTrack">Do not track preference</param>
/// <param name="AppCodeName">Application code name</param>
/// <param name="AppName">Application name</param>
/// <param name="AppVersion">Application version</param>
/// <param name="Oscpu">Operating system CPU information</param>
/// <param name="Webdriver">Whether webdriver is present</param>
/// <param name="Language">Primary language</param>
/// <param name="Languages">List of supported languages</param>
/// <param name="Platform">Operating system platform</param>
/// <param name="DeviceMemory">Device memory in gigabytes</param>
/// <param name="HardwareConcurrency">Number of logical processors</param>
/// <param name="Product">Product name</param>
/// <param name="ProductSub">Product sub-version</param>
/// <param name="Vendor">Browser vendor</param>
/// <param name="VendorSub">Vendor sub-version</param>
/// <param name="MaxTouchPoints">Maximum number of touch points</param>
/// <param name="ExtraProperties">Additional navigator properties</param>
public record NavigatorFingerprint(
    [property: JsonPropertyName("userAgent")] string UserAgent,
    [property: JsonPropertyName("userAgentData")] Dictionary<string, object> UserAgentData,
    [property: JsonPropertyName("doNotTrack")] string? DoNotTrack,
    [property: JsonPropertyName("appCodeName")] string AppCodeName,
    [property: JsonPropertyName("appName")] string AppName,
    [property: JsonPropertyName("appVersion")] string AppVersion,
    [property: JsonPropertyName("oscpu")] string Oscpu,
    [property: JsonPropertyName("webdriver")] string Webdriver,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("languages")] List<string> Languages,
    [property: JsonPropertyName("platform")] string Platform,
    [property: JsonPropertyName("deviceMemory")] int? DeviceMemory,
    [property: JsonPropertyName("hardwareConcurrency")] int HardwareConcurrency,
    [property: JsonPropertyName("product")] string Product,
    [property: JsonPropertyName("productSub")] string ProductSub,
    [property: JsonPropertyName("vendor")] string Vendor,
    [property: JsonPropertyName("vendorSub")] string VendorSub,
    [property: JsonPropertyName("maxTouchPoints")] int MaxTouchPoints,
    [property: JsonPropertyName("extraProperties")] Dictionary<string, object> ExtraProperties
)
{
    /// <summary>
    /// Creates a NavigatorFingerprint instance from a dictionary representation
    /// </summary>
    /// <param name="data">Dictionary containing navigator properties</param>
    /// <returns>NavigatorFingerprint instance</returns>
    public static NavigatorFingerprint FromDictionary(Dictionary<string, object> data)
    {
        return new NavigatorFingerprint(
            UserAgent: GetStringValue(data, "userAgent") ?? string.Empty,
            UserAgentData: GetDictionaryValue(data, "userAgentData") ?? new Dictionary<string, object>(),
            DoNotTrack: GetStringValue(data, "doNotTrack"),
            AppCodeName: GetStringValue(data, "appCodeName") ?? "Mozilla",
            AppName: GetStringValue(data, "appName") ?? "Netscape",
            AppVersion: GetStringValue(data, "appVersion") ?? string.Empty,
            Oscpu: GetStringValue(data, "oscpu") ?? string.Empty,
            Webdriver: GetStringValue(data, "webdriver") ?? "false",
            Language: GetStringValue(data, "language") ?? "en-US",
            Languages: GetListValue(data, "languages") ?? new List<string> { "en-US", "en" },
            Platform: GetStringValue(data, "platform") ?? "Win32",
            DeviceMemory: GetIntValue(data, "deviceMemory"),
            HardwareConcurrency: GetIntValue(data, "hardwareConcurrency") ?? Environment.ProcessorCount,
            Product: GetStringValue(data, "product") ?? "Gecko",
            ProductSub: GetStringValue(data, "productSub") ?? "20030107",
            Vendor: GetStringValue(data, "vendor") ?? string.Empty,
            VendorSub: GetStringValue(data, "vendorSub") ?? string.Empty,
            MaxTouchPoints: GetIntValue(data, "maxTouchPoints") ?? 0,
            ExtraProperties: GetDictionaryValue(data, "extraProperties") ?? new Dictionary<string, object>()
        );
    }

    /// <summary>
    /// Creates a NavigatorFingerprint for Chrome browser
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <param name="platform">Operating system platform</param>
    /// <param name="languages">Supported languages</param>
    /// <returns>NavigatorFingerprint configured for Chrome</returns>
    public static NavigatorFingerprint CreateChrome(string userAgent, string platform = "Win32", List<string>? languages = null)
    {
        languages ??= new List<string> { "en-US", "en" };
        
        var userAgentData = new Dictionary<string, object>
        {
            { "brands", new List<Dictionary<string, object>>
                {
                    new() { { "brand", "Not_A Brand" }, { "version", "8" } },
                    new() { { "brand", "Chromium" }, { "version", "108" } },
                    new() { { "brand", "Google Chrome" }, { "version", "108" } }
                }
            },
            { "mobile", false },
            { "platform", platform }
        };

        return new NavigatorFingerprint(
            UserAgent: userAgent,
            UserAgentData: userAgentData,
            DoNotTrack: null,
            AppCodeName: "Mozilla",
            AppName: "Netscape",
            AppVersion: userAgent.Substring(userAgent.IndexOf('/') + 1),
            Oscpu: GetOscpuFromPlatform(platform),
            Webdriver: "false",
            Language: languages.FirstOrDefault() ?? "en-US",
            Languages: languages,
            Platform: platform,
            DeviceMemory: Random.Shared.Next(4, 17), // 4-16 GB
            HardwareConcurrency: Environment.ProcessorCount,
            Product: "Gecko",
            ProductSub: "20030107",
            Vendor: "Google Inc.",
            VendorSub: string.Empty,
            MaxTouchPoints: 0,
            ExtraProperties: new Dictionary<string, object>()
        );
    }

    /// <summary>
    /// Creates a NavigatorFingerprint for Firefox browser
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <param name="platform">Operating system platform</param>
    /// <param name="languages">Supported languages</param>
    /// <returns>NavigatorFingerprint configured for Firefox</returns>
    public static NavigatorFingerprint CreateFirefox(string userAgent, string platform = "Win32", List<string>? languages = null)
    {
        languages ??= new List<string> { "en-US", "en" };

        return new NavigatorFingerprint(
            UserAgent: userAgent,
            UserAgentData: new Dictionary<string, object>(),
            DoNotTrack: "unspecified",
            AppCodeName: "Mozilla",
            AppName: "Netscape",
            AppVersion: userAgent.Substring(userAgent.IndexOf('/') + 1),
            Oscpu: GetOscpuFromPlatform(platform),
            Webdriver: "false",
            Language: languages.FirstOrDefault() ?? "en-US",
            Languages: languages,
            Platform: platform,
            DeviceMemory: null, // Firefox doesn't expose this
            HardwareConcurrency: Environment.ProcessorCount,
            Product: "Gecko",
            ProductSub: "20100101",
            Vendor: string.Empty,
            VendorSub: string.Empty,
            MaxTouchPoints: 0,
            ExtraProperties: new Dictionary<string, object>()
        );
    }

    private static string? GetStringValue(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static int? GetIntValue(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            int intVal => intVal,
            long longVal => (int)longVal,
            double doubleVal => (int)doubleVal,
            float floatVal => (int)floatVal,
            string strVal when int.TryParse(strVal, out var parsed) => parsed,
            _ => null
        };
    }

    private static List<string>? GetListValue(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            List<string> stringList => stringList,
            List<object> objectList => objectList.Select(o => o?.ToString() ?? string.Empty).ToList(),
            string[] stringArray => stringArray.ToList(),
            object[] objectArray => objectArray.Select(o => o?.ToString() ?? string.Empty).ToList(),
            _ => null
        };
    }

    private static Dictionary<string, object>? GetDictionaryValue(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value))
            return null;

        return value as Dictionary<string, object>;
    }

    private static string GetOscpuFromPlatform(string platform)
    {
        return platform switch
        {
            "Win32" => "Windows NT 10.0; Win64; x64",
            "MacIntel" => "Intel Mac OS X 10.15",
            "Linux x86_64" => "X11; Linux x86_64",
            "Linux i686" => "X11; Linux i686",
            _ => platform
        };
    }
}