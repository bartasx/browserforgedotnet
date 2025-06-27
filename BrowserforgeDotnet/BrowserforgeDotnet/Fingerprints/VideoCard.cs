using System.Text.Json.Serialization;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Represents GPU/graphics card information for browser fingerprinting
/// </summary>
/// <param name="Renderer">The renderer string from WebGL context</param>
/// <param name="Vendor">The vendor string from WebGL context</param>
public record VideoCard(
    [property: JsonPropertyName("renderer")] string Renderer,
    [property: JsonPropertyName("vendor")] string Vendor
)
{
    /// <summary>
    /// Creates a VideoCard instance from a dictionary representation
    /// </summary>
    /// <param name="data">Dictionary containing renderer and vendor information</param>
    /// <returns>VideoCard instance</returns>
    public static VideoCard FromDictionary(Dictionary<string, object> data)
    {
        var renderer = data.TryGetValue("renderer", out var rendererObj) ? rendererObj?.ToString() ?? string.Empty : string.Empty;
        var vendor = data.TryGetValue("vendor", out var vendorObj) ? vendorObj?.ToString() ?? string.Empty : string.Empty;
        
        return new VideoCard(renderer, vendor);
    }

    /// <summary>
    /// Creates a default VideoCard with common values
    /// </summary>
    /// <returns>Default VideoCard instance</returns>
    public static VideoCard CreateDefault()
    {
        return new VideoCard(
            "ANGLE (Intel, Intel(R) HD Graphics 620 Direct3D11 vs_5_0 ps_5_0, D3D11-27.20.100.8179)",
            "Google Inc. (Intel)"
        );
    }
}