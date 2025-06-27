namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Constants used throughout the fingerprint generation system
/// </summary>
public static class FingerprintConstants
{
    /// <summary>
    /// Token representing missing values in the dataset
    /// </summary>
    public const string MissingValueDatasetToken = "*MISSING_VALUE*";

    /// <summary>
    /// Prefix for stringified JSON values in the dataset
    /// </summary>
    public const string StringifiedPrefix = "*STRINGIFIED*";

    /// <summary>
    /// Common video codecs supported by browsers
    /// </summary>
    public static readonly Dictionary<string, string> VideoCodecs = new()
    {
        { "video/mp4; codecs=\"avc1.42E01E\"", "probably" },
        { "video/mp4; codecs=\"avc1.64001E\"", "probably" },
        { "video/mp4; codecs=\"mp4v.20.8\"", "probably" },
        { "video/mp4; codecs=\"mp4v.20.240\"", "probably" },
        { "video/mp4; codecs=\"avc1.4D401E\"", "probably" },
        { "video/webm; codecs=\"vp8, vorbis\"", "probably" },
        { "video/webm; codecs=\"vp9\"", "probably" },
        { "video/ogg; codecs=\"theora\"", "probably" },
        { "video/3gpp; codecs=\"mp4v.20.8\"", "probably" },
        { "video/x-msvideo", "maybe" }
    };

    /// <summary>
    /// Common audio codecs supported by browsers
    /// </summary>
    public static readonly Dictionary<string, string> AudioCodecs = new()
    {
        { "audio/mp4; codecs=\"mp4a.40.2\"", "probably" },
        { "audio/mpeg", "probably" },
        { "audio/webm; codecs=\"vorbis\"", "probably" },
        { "audio/ogg; codecs=\"vorbis\"", "probably" },
        { "audio/wav; codecs=\"1\"", "probably" },
        { "audio/flac", "probably" },
        { "audio/x-m4a", "maybe" },
        { "audio/aac", "probably" }
    };

    /// <summary>
    /// Common browser plugins and their descriptions
    /// </summary>
    public static readonly Dictionary<string, string> CommonPlugins = new()
    {
        { "Chrome PDF Plugin", "Portable Document Format" },
        { "Chrome PDF Viewer", "Portable Document Format" },
        { "Native Client", "Native Client Executable" },
        { "Widevine Content Decryption Module", "Enables Widevine licenses for playback of HTML audio/video content" },
        { "Microsoft Edge PDF Plugin", "Portable Document Format" },
        { "WebKit built-in PDF", "Portable Document Format" }
    };

    /// <summary>
    /// Common multimedia devices that might be enumerated
    /// </summary>
    public static readonly List<string> MultimediaDevices = new()
    {
        "Default - Microphone (Realtek Audio)",
        "Default - Speakers (Realtek Audio)",
        "Microphone (Realtek Audio)",
        "Speakers (Realtek Audio)",
        "Communications - Microphone (Realtek Audio)",
        "Communications - Speakers (Realtek Audio)"
    };

    /// <summary>
    /// Common font families available in browsers
    /// </summary>
    public static readonly List<string> CommonFonts = new()
    {
        "Arial", "Arial Black", "Arial Narrow", "Arial Unicode MS",
        "Calibri", "Cambria", "Cambria Math", "Comic Sans MS",
        "Consolas", "Courier", "Courier New", "Georgia",
        "Helvetica", "Impact", "Lucida Console", "Lucida Sans Unicode",
        "Microsoft Sans Serif", "Palatino Linotype", "Segoe UI",
        "Tahoma", "Times", "Times New Roman", "Trebuchet MS",
        "Verdana", "Webdings", "Wingdings"
    };

    /// <summary>
    /// Default battery information for simulated battery status
    /// </summary>
    public static readonly Dictionary<string, object> DefaultBatteryInfo = new()
    {
        { "charging", true },
        { "chargingTime", double.PositiveInfinity },
        { "dischargingTime", double.PositiveInfinity },
        { "level", 1.0 }
    };

    /// <summary>
    /// Sec-Fetch attributes for different HTTP versions
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, string>> SecFetchAttributes = new()
    {
        {
            "http1", new Dictionary<string, string>
            {
                { "Sec-Fetch-Dest", "document" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-Site", "none" },
                { "Sec-Fetch-User", "?1" }
            }
        },
        {
            "http2", new Dictionary<string, string>
            {
                { "sec-fetch-dest", "document" },
                { "sec-fetch-mode", "navigate" },
                { "sec-fetch-site", "none" },
                { "sec-fetch-user", "?1" }
            }
        }
    };

    /// <summary>
    /// Common screen resolutions with their properties
    /// </summary>
    public static readonly List<(int Width, int Height, float DevicePixelRatio)> CommonResolutions = new()
    {
        (1920, 1080, 1.0f),
        (1366, 768, 1.0f),
        (1440, 900, 1.0f),
        (1600, 900, 1.0f),
        (1280, 1024, 1.0f),
        (1024, 768, 1.0f),
        (1680, 1050, 1.0f),
        (1280, 800, 1.0f),
        (1920, 1200, 1.0f),
        (2560, 1440, 1.0f),
        (3840, 2160, 1.0f), // 4K
        (2560, 1600, 2.0f), // High DPI
        (1280, 720, 1.5f)   // Mobile/tablet
    };

    /// <summary>
    /// WebRTC IP addresses for mocking
    /// </summary>
    public static readonly List<string> WebRtcIpAddresses = new()
    {
        "192.168.1.1",
        "192.168.0.1",
        "10.0.0.1",
        "172.16.0.1",
        "127.0.0.1"
    };
}