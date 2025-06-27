using System.Text.Json.Serialization;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Represents screen dimensions and properties for browser fingerprinting
/// </summary>
/// <param name="AvailHeight">Available screen height (excluding taskbars, etc.)</param>
/// <param name="AvailWidth">Available screen width (excluding taskbars, etc.)</param>
/// <param name="AvailTop">Top position of available screen area</param>
/// <param name="AvailLeft">Left position of available screen area</param>
/// <param name="ColorDepth">Color depth of the screen in bits</param>
/// <param name="Height">Full screen height</param>
/// <param name="PixelDepth">Pixel depth of the screen in bits</param>
/// <param name="Width">Full screen width</param>
/// <param name="DevicePixelRatio">Device pixel ratio for high-DPI displays</param>
/// <param name="PageXOffset">Horizontal scroll offset of the page</param>
/// <param name="PageYOffset">Vertical scroll offset of the page</param>
/// <param name="InnerHeight">Inner height of the browser window</param>
/// <param name="OuterHeight">Outer height of the browser window</param>
/// <param name="OuterWidth">Outer width of the browser window</param>
/// <param name="InnerWidth">Inner width of the browser window</param>
/// <param name="ScreenX">X coordinate of the browser window</param>
/// <param name="ClientWidth">Width of the document's viewport</param>
/// <param name="ClientHeight">Height of the document's viewport</param>
/// <param name="HasHDR">Whether the screen supports HDR</param>
public record ScreenFingerprint(
    [property: JsonPropertyName("availHeight")] int AvailHeight,
    [property: JsonPropertyName("availWidth")] int AvailWidth,
    [property: JsonPropertyName("availTop")] int AvailTop,
    [property: JsonPropertyName("availLeft")] int AvailLeft,
    [property: JsonPropertyName("colorDepth")] int ColorDepth,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("pixelDepth")] int PixelDepth,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("devicePixelRatio")] float DevicePixelRatio,
    [property: JsonPropertyName("pageXOffset")] int PageXOffset,
    [property: JsonPropertyName("pageYOffset")] int PageYOffset,
    [property: JsonPropertyName("innerHeight")] int InnerHeight,
    [property: JsonPropertyName("outerHeight")] int OuterHeight,
    [property: JsonPropertyName("outerWidth")] int OuterWidth,
    [property: JsonPropertyName("innerWidth")] int InnerWidth,
    [property: JsonPropertyName("screenX")] int ScreenX,
    [property: JsonPropertyName("clientWidth")] int ClientWidth,
    [property: JsonPropertyName("clientHeight")] int ClientHeight,
    [property: JsonPropertyName("hasHDR")] bool HasHDR
)
{
    /// <summary>
    /// Creates a ScreenFingerprint instance from a dictionary representation
    /// </summary>
    /// <param name="data">Dictionary containing screen properties</param>
    /// <returns>ScreenFingerprint instance</returns>
    public static ScreenFingerprint FromDictionary(Dictionary<string, object> data)
    {
        return new ScreenFingerprint(
            AvailHeight: GetIntValue(data, "availHeight"),
            AvailWidth: GetIntValue(data, "availWidth"),
            AvailTop: GetIntValue(data, "availTop"),
            AvailLeft: GetIntValue(data, "availLeft"),
            ColorDepth: GetIntValue(data, "colorDepth", 24),
            Height: GetIntValue(data, "height"),
            PixelDepth: GetIntValue(data, "pixelDepth", 24),
            Width: GetIntValue(data, "width"),
            DevicePixelRatio: GetFloatValue(data, "devicePixelRatio", 1.0f),
            PageXOffset: GetIntValue(data, "pageXOffset", 0),
            PageYOffset: GetIntValue(data, "pageYOffset", 0),
            InnerHeight: GetIntValue(data, "innerHeight"),
            OuterHeight: GetIntValue(data, "outerHeight"),
            OuterWidth: GetIntValue(data, "outerWidth"),
            InnerWidth: GetIntValue(data, "innerWidth"),
            ScreenX: GetIntValue(data, "screenX", 0),
            ClientWidth: GetIntValue(data, "clientWidth"),
            ClientHeight: GetIntValue(data, "clientHeight"),
            HasHDR: GetBoolValue(data, "hasHDR", false)
        );
    }

    /// <summary>
    /// Creates a ScreenFingerprint based on common resolution and device pixel ratio
    /// </summary>
    /// <param name="width">Screen width</param>
    /// <param name="height">Screen height</param>
    /// <param name="devicePixelRatio">Device pixel ratio</param>
    /// <returns>ScreenFingerprint instance with calculated properties</returns>
    public static ScreenFingerprint Create(int width, int height, float devicePixelRatio = 1.0f)
    {
        // Calculate typical browser chrome dimensions
        var taskbarHeight = 40; // Typical taskbar height
        var browserChromeHeight = 85; // Typical browser UI height (address bar, tabs, etc.)
        var browserChromeWidth = 16; // Typical browser UI width (scrollbars, etc.)

        var availHeight = height - taskbarHeight;
        var availWidth = width;
        var innerHeight = availHeight - browserChromeHeight;
        var innerWidth = availWidth - browserChromeWidth;

        return new ScreenFingerprint(
            AvailHeight: availHeight,
            AvailWidth: availWidth,
            AvailTop: 0,
            AvailLeft: 0,
            ColorDepth: 24,
            Height: height,
            PixelDepth: 24,
            Width: width,
            DevicePixelRatio: devicePixelRatio,
            PageXOffset: 0,
            PageYOffset: 0,
            InnerHeight: innerHeight,
            OuterHeight: innerHeight + browserChromeHeight,
            OuterWidth: innerWidth + browserChromeWidth,
            InnerWidth: innerWidth,
            ScreenX: 0,
            ClientWidth: innerWidth,
            ClientHeight: innerHeight,
            HasHDR: devicePixelRatio > 1.5f // Assume HDR for high-DPI displays
        );
    }

    private static int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
    {
        if (!data.TryGetValue(key, out var value))
            return defaultValue;

        return value switch
        {
            int intVal => intVal,
            long longVal => (int)longVal,
            double doubleVal => (int)doubleVal,
            float floatVal => (int)floatVal,
            string strVal when int.TryParse(strVal, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static float GetFloatValue(Dictionary<string, object> data, string key, float defaultValue = 0.0f)
    {
        if (!data.TryGetValue(key, out var value))
            return defaultValue;

        return value switch
        {
            float floatVal => floatVal,
            double doubleVal => (float)doubleVal,
            int intVal => (float)intVal,
            long longVal => (float)longVal,
            string strVal when float.TryParse(strVal, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
    {
        if (!data.TryGetValue(key, out var value))
            return defaultValue;

        return value switch
        {
            bool boolVal => boolVal,
            string strVal when bool.TryParse(strVal, out var parsed) => parsed,
            int intVal => intVal != 0,
            _ => defaultValue
        };
    }
}