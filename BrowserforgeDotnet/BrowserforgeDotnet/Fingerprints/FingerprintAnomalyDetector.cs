using System.Text.RegularExpressions;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Advanced anomaly detection system for identifying statistical outliers and suspicious patterns in fingerprints
/// </summary>
public static class FingerprintAnomalyDetector
{
    // Known device profiles for validation
    private static readonly Dictionary<string, DeviceProfile> CommonDeviceProfiles = new()
    {
        // Desktop resolutions
        { "1920x1080", new DeviceProfile(1920, 1080, 1.0f, "desktop", new[] { "Win32", "MacIntel", "Linux x86_64" }) },
        { "1366x768", new DeviceProfile(1366, 768, 1.0f, "desktop", new[] { "Win32", "Linux x86_64" }) },
        { "1440x900", new DeviceProfile(1440, 900, 1.0f, "desktop", new[] { "MacIntel", "Win32" }) },
        { "2560x1440", new DeviceProfile(2560, 1440, 1.0f, "desktop", new[] { "Win32", "MacIntel", "Linux x86_64" }) },
        { "3840x2160", new DeviceProfile(3840, 2160, 1.0f, "desktop", new[] { "Win32", "MacIntel" }) },
        
        // Mac Retina displays
        { "2880x1800", new DeviceProfile(2880, 1800, 2.0f, "desktop", new[] { "MacIntel" }) },
        { "5120x2880", new DeviceProfile(5120, 2880, 2.0f, "desktop", new[] { "MacIntel" }) },
        
        // Mobile resolutions
        { "375x667", new DeviceProfile(375, 667, 2.0f, "mobile", new[] { "iPhone" }) },
        { "414x896", new DeviceProfile(414, 896, 3.0f, "mobile", new[] { "iPhone" }) },
        { "360x640", new DeviceProfile(360, 640, 2.0f, "mobile", new[] { "Linux armv7l" }) }
    };

    // Statistical thresholds for outlier detection
    private static readonly Dictionary<string, (double mean, double stdDev)> StatisticalNorms = new()
    {
        { "deviceMemory", (8.2, 4.1) },
        { "hardwareConcurrency", (8.0, 4.0) },
        { "fontCount", (45.0, 25.0) },
        { "codecCount", (12.0, 4.0) },
        { "multimediaDeviceCount", (4.0, 2.0) },
        { "languageCount", (2.3, 1.2) }
    };

    /// <summary>
    /// Detects statistical outliers and suspicious patterns in the fingerprint
    /// </summary>
    /// <param name="fingerprint">Fingerprint to analyze</param>
    /// <param name="referenceDataset">Optional reference dataset for comparison</param>
    /// <returns>List of detected anomalies</returns>
    public static List<AnomalyReport> DetectAnomalies(Fingerprint fingerprint, List<Fingerprint>? referenceDataset = null)
    {
        var anomalies = new List<AnomalyReport>();

        // Detect statistical outliers
        DetectStatisticalOutliers(fingerprint, anomalies);
        
        // Detect impossible hardware combinations
        DetectImpossibleHardware(fingerprint, anomalies);
        
        // Check for too perfect/common patterns
        DetectTooCommonPatterns(fingerprint, anomalies, referenceDataset);
        
        // Check for automated generation patterns
        DetectAutomatedGeneration(fingerprint, anomalies);
        
        // Validate against known device profiles
        ValidateAgainstDeviceProfiles(fingerprint, anomalies);
        
        // Detect temporal inconsistencies
        DetectTemporalInconsistencies(fingerprint, anomalies);

        return anomalies;
    }

    /// <summary>
    /// Detects values that are statistical outliers compared to normal distributions
    /// </summary>
    private static void DetectStatisticalOutliers(Fingerprint fingerprint, List<AnomalyReport> anomalies)
    {
        // Check device memory outliers
        if (fingerprint.Navigator.DeviceMemory.HasValue)
        {
            var memory = fingerprint.Navigator.DeviceMemory.Value;
            var (mean, stdDev) = StatisticalNorms["deviceMemory"];
            var zScore = Math.Abs((memory - mean) / stdDev);

            if (zScore > 2.5) // More than 2.5 standard deviations
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.StatisticalOutlier,
                    Severity: zScore > 3.0 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description: $"Device memory {memory}GB is a statistical outlier (z-score: {zScore:F2})",
                    Field: "navigator.deviceMemory",
                    ExpectedValue: $"Typical range: {mean - 2 * stdDev:F1}-{mean + 2 * stdDev:F1}GB",
                    ActualValue: $"{memory}GB",
                    SuspiciousnessContribution: Math.Min(20, (int)(zScore * 5)),
                    Recommendation: "Use more common device memory values"
                ));
            }
        }

        // Check hardware concurrency outliers
        var cores = fingerprint.Navigator.HardwareConcurrency;
        var (coreMean, coreStdDev) = StatisticalNorms["hardwareConcurrency"];
        var coreZScore = Math.Abs((cores - coreMean) / coreStdDev);

        if (coreZScore > 2.0)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.StatisticalOutlier,
                Severity: coreZScore > 3.0 ? AnomalySeverity.High : AnomalySeverity.Medium,
                Description: $"Hardware concurrency {cores} is a statistical outlier (z-score: {coreZScore:F2})",
                Field: "navigator.hardwareConcurrency",
                ExpectedValue: $"Typical range: {coreMean - 2 * coreStdDev:F0}-{coreMean + 2 * coreStdDev:F0}",
                ActualValue: cores.ToString(),
                SuspiciousnessContribution: Math.Min(15, (int)(coreZScore * 4)),
                Recommendation: "Use more common CPU core counts"
            ));
        }

        // Check font count outliers
        var fontCount = fingerprint.Fonts.Count;
        var (fontMean, fontStdDev) = StatisticalNorms["fontCount"];
        var fontZScore = Math.Abs((fontCount - fontMean) / fontStdDev);

        if (fontZScore > 2.0)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.StatisticalOutlier,
                Severity: fontZScore > 3.0 ? AnomalySeverity.Medium : AnomalySeverity.Low,
                Description: $"Font count {fontCount} is a statistical outlier (z-score: {fontZScore:F2})",
                Field: "fonts",
                ExpectedValue: $"Typical range: {fontMean - 2 * fontStdDev:F0}-{fontMean + 2 * fontStdDev:F0}",
                ActualValue: fontCount.ToString(),
                SuspiciousnessContribution: Math.Min(8, (int)(fontZScore * 2)),
                Recommendation: "Use more typical font counts"
            ));
        }

        // Check codec count outliers
        var totalCodecs = fingerprint.VideoCodecs.Count + fingerprint.AudioCodecs.Count;
        var (codecMean, codecStdDev) = StatisticalNorms["codecCount"];
        var codecZScore = Math.Abs((totalCodecs - codecMean) / codecStdDev);

        if (codecZScore > 2.0)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.StatisticalOutlier,
                Severity: AnomalySeverity.Low,
                Description: $"Total codec count {totalCodecs} is a statistical outlier (z-score: {codecZScore:F2})",
                Field: "codecs",
                ExpectedValue: $"Typical range: {codecMean - 2 * codecStdDev:F0}-{codecMean + 2 * codecStdDev:F0}",
                ActualValue: totalCodecs.ToString(),
                SuspiciousnessContribution: Math.Min(6, (int)(codecZScore * 2)),
                Recommendation: "Use more typical codec counts"
            ));
        }
    }

    /// <summary>
    /// Detects impossible or highly unlikely hardware combinations
    /// </summary>
    private static void DetectImpossibleHardware(Fingerprint fingerprint, List<AnomalyReport> anomalies)
    {
        var screen = fingerprint.Screen;
        var navigator = fingerprint.Navigator;

        // Detect impossible screen resolution combinations
        var aspectRatio = (double)screen.Width / screen.Height;
        if (aspectRatio < 0.5 || aspectRatio > 5.0)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ImpossibleHardware,
                Severity: AnomalySeverity.High,
                Description: $"Screen aspect ratio {aspectRatio:F2} is unrealistic",
                Field: "screen.dimensions",
                ExpectedValue: "0.5-5.0 aspect ratio",
                ActualValue: $"{aspectRatio:F2}",
                SuspiciousnessContribution: 25,
                Recommendation: "Use realistic screen aspect ratios"
            ));
        }

        // Detect impossible device pixel ratios
        if (screen.DevicePixelRatio < 0.5f || screen.DevicePixelRatio > 5.0f)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ImpossibleHardware,
                Severity: AnomalySeverity.High,
                Description: $"Device pixel ratio {screen.DevicePixelRatio} is outside possible range",
                Field: "screen.devicePixelRatio",
                ExpectedValue: "0.5-5.0",
                ActualValue: screen.DevicePixelRatio.ToString(),
                SuspiciousnessContribution: 20,
                Recommendation: "Use realistic device pixel ratios"
            ));
        }

        // Detect impossible memory/CPU combinations
        if (navigator.DeviceMemory.HasValue)
        {
            var memory = navigator.DeviceMemory.Value;
            var cores = navigator.HardwareConcurrency;

            // Very low-end: 1-2 cores should have 2-4GB max
            if (cores <= 2 && memory > 8)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ImpossibleHardware,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Low CPU core count ({cores}) with high memory ({memory}GB) is unlikely",
                    Field: "hardware_combination",
                    ExpectedValue: "2-8GB for 1-2 cores",
                    ActualValue: $"{cores} cores, {memory}GB RAM",
                    SuspiciousnessContribution: 12,
                    Recommendation: "Ensure CPU and memory specifications are proportional"
                ));
            }

            // High-end: Many cores with very little memory is impossible
            if (cores >= 16 && memory < 8)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ImpossibleHardware,
                    Severity: AnomalySeverity.High,
                    Description: $"High CPU core count ({cores}) with low memory ({memory}GB) is impossible",
                    Field: "hardware_combination",
                    ExpectedValue: "At least 16GB for 16+ cores",
                    ActualValue: $"{cores} cores, {memory}GB RAM",
                    SuspiciousnessContribution: 30,
                    Recommendation: "High-core systems require proportional memory"
                ));
            }
        }

        // Detect impossible screen/memory combinations for mobile
        var platform = navigator.Platform.ToLowerInvariant();
        if ((platform.Contains("iphone") || platform.Contains("android")) && navigator.DeviceMemory.HasValue)
        {
            var memory = navigator.DeviceMemory.Value;
            if (memory > 12)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ImpossibleHardware,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Mobile device with {memory}GB memory is uncommon",
                    Field: "navigator.deviceMemory",
                    ExpectedValue: "2-12GB for mobile devices",
                    ActualValue: $"{memory}GB",
                    SuspiciousnessContribution: 8,
                    Recommendation: "Use typical mobile device memory ranges"
                ));
            }
        }
    }

    /// <summary>
    /// Detects fingerprints that are too perfect or too common (might be easily flagged)
    /// </summary>
    private static void DetectTooCommonPatterns(Fingerprint fingerprint, List<AnomalyReport> anomalies, List<Fingerprint>? referenceDataset)
    {
        // Check for too perfect screen dimensions
        var perfectResolutions = new[]
        {
            (1920, 1080), (1366, 768), (1440, 900), (1280, 720), (1600, 900),
            (2560, 1440), (3840, 2160), (1280, 1024)
        };

        var hasExactMatch = perfectResolutions.Any(res => 
            res.Item1 == fingerprint.Screen.Width && res.Item2 == fingerprint.Screen.Height);

        if (hasExactMatch)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.TooCommon,
                Severity: AnomalySeverity.Low,
                Description: $"Screen resolution {fingerprint.Screen.Width}x{fingerprint.Screen.Height} is extremely common",
                Field: "screen.dimensions",
                ExpectedValue: "Slightly varied dimensions",
                ActualValue: $"{fingerprint.Screen.Width}x{fingerprint.Screen.Height}",
                SuspiciousnessContribution: 5,
                Recommendation: "Consider using slightly varied screen dimensions"
            ));
        }

        // Check for too perfect device pixel ratios
        var perfectRatios = new[] { 1.0f, 1.25f, 1.5f, 2.0f, 3.0f };
        if (perfectRatios.Contains(fingerprint.Screen.DevicePixelRatio))
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.TooCommon,
                Severity: AnomalySeverity.Low,
                Description: $"Device pixel ratio {fingerprint.Screen.DevicePixelRatio} is very common",
                Field: "screen.devicePixelRatio",
                ExpectedValue: "Slightly varied ratios",
                ActualValue: fingerprint.Screen.DevicePixelRatio.ToString(),
                SuspiciousnessContribution: 3,
                Recommendation: "Consider using slightly varied device pixel ratios"
            ));
        }

        // Check for default User-Agent patterns
        var userAgent = fingerprint.Navigator.UserAgent;
        if (userAgent.Contains("Mozilla/5.0") && !userAgent.Contains("("))
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.TooCommon,
                Severity: AnomalySeverity.Medium,
                Description: "User-Agent appears to be a basic template",
                Field: "navigator.userAgent",
                ExpectedValue: "Realistic User-Agent with system details",
                ActualValue: userAgent.Substring(0, Math.Min(50, userAgent.Length)) + "...",
                SuspiciousnessContribution: 10,
                Recommendation: "Use more detailed and realistic User-Agent strings"
            ));
        }

        // Check for round numbers in device memory
        if (fingerprint.Navigator.DeviceMemory.HasValue)
        {
            var memory = fingerprint.Navigator.DeviceMemory.Value;
            var roundNumbers = new[] { 4, 8, 16, 32 };
            if (roundNumbers.Contains(memory))
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.TooCommon,
                    Severity: AnomalySeverity.Low,
                    Description: $"Device memory {memory}GB is a very round number",
                    Field: "navigator.deviceMemory",
                    ExpectedValue: "Varied memory amounts",
                    ActualValue: $"{memory}GB",
                    SuspiciousnessContribution: 2,
                    Recommendation: "Consider using varied memory amounts like 6GB or 12GB"
                ));
            }
        }
    }

    /// <summary>
    /// Detects patterns that might indicate automated fingerprint generation
    /// </summary>
    private static void DetectAutomatedGeneration(Fingerprint fingerprint, List<AnomalyReport> anomalies)
    {
        // Check for sequential or pattern-based values
        var fonts = fingerprint.Fonts;
        if (fonts.Count >= 3)
        {
            // Check if fonts are in alphabetical order (suspicious)
            var sortedFonts = fonts.OrderBy(f => f).ToList();
            if (fonts.SequenceEqual(sortedFonts))
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.AutomatedGeneration,
                    Severity: AnomalySeverity.Medium,
                    Description: "Font list is in perfect alphabetical order",
                    Field: "fonts",
                    ExpectedValue: "Naturally ordered font list",
                    ActualValue: "Alphabetically sorted",
                    SuspiciousnessContribution: 15,
                    Recommendation: "Randomize font order to appear more natural"
                ));
            }
        }

        // Check for suspiciously round timing values
        if (fingerprint.Battery != null)
        {
            if (fingerprint.Battery.TryGetValue("chargingTime", out var chargingTimeObj) && 
                chargingTimeObj is double chargingTime && 
                !double.IsInfinity(chargingTime))
            {
                // Check if timing is a round number
                if (chargingTime % 1800 == 0) // Multiple of 30 minutes
                {
                    anomalies.Add(new AnomalyReport(
                        Type: AnomalyType.AutomatedGeneration,
                        Severity: AnomalySeverity.Low,
                        Description: $"Battery charging time {chargingTime}s is suspiciously round",
                        Field: "battery.chargingTime",
                        ExpectedValue: "Varied timing values",
                        ActualValue: $"{chargingTime}s",
                        SuspiciousnessContribution: 4,
                        Recommendation: "Use more varied battery timing values"
                    ));
                }
            }
        }

        // Check for template-like language patterns
        var languages = fingerprint.Navigator.Languages;
        if (languages.Count == 2 && languages[0] == "en-US" && languages[1] == "en")
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.AutomatedGeneration,
                Severity: AnomalySeverity.Low,
                Description: "Language configuration appears to be a default template",
                Field: "navigator.languages",
                ExpectedValue: "Varied language preferences",
                ActualValue: string.Join(", ", languages),
                SuspiciousnessContribution: 6,
                Recommendation: "Use more varied language configurations"
            ));
        }

        // Check for identical codec lists (common in automated generation)
        var videoCodecKeys = fingerprint.VideoCodecs.Keys.OrderBy(k => k).ToList();
        var audioCodecKeys = fingerprint.AudioCodecs.Keys.OrderBy(k => k).ToList();
        
        // Common automated codec patterns
        var commonVideoCodecs = new[] { "video/mp4", "video/webm" }.OrderBy(c => c).ToList();
        var commonAudioCodecs = new[] { "audio/mpeg", "audio/ogg", "audio/wav", "audio/webm" }.OrderBy(c => c).ToList();

        if (videoCodecKeys.SequenceEqual(commonVideoCodecs) && audioCodecKeys.SequenceEqual(commonAudioCodecs))
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.AutomatedGeneration,
                Severity: AnomalySeverity.Low,
                Description: "Codec configuration appears to be a common template",
                Field: "codecs",
                ExpectedValue: "Varied codec support",
                ActualValue: "Standard template configuration",
                SuspiciousnessContribution: 7,
                Recommendation: "Vary codec support based on browser and platform"
            ));
        }
    }

    /// <summary>
    /// Validates screen dimensions against known device profiles
    /// </summary>
    private static void ValidateAgainstDeviceProfiles(Fingerprint fingerprint, List<AnomalyReport> anomalies)
    {
        var screen = fingerprint.Screen;
        var platform = fingerprint.Navigator.Platform;
        var key = $"{screen.Width}x{screen.Height}";

        if (CommonDeviceProfiles.TryGetValue(key, out var profile))
        {
            // Check if platform matches expected platforms for this resolution
            if (!profile.CompatiblePlatforms.Contains(platform))
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ScreenInconsistency,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Screen resolution {key} is not typical for platform {platform}",
                    Field: "screen.dimensions",
                    ExpectedValue: string.Join(", ", profile.CompatiblePlatforms),
                    ActualValue: platform,
                    SuspiciousnessContribution: 10,
                    Recommendation: $"Use platforms compatible with {key}: {string.Join(", ", profile.CompatiblePlatforms)}"
                ));
            }

            // Check if device pixel ratio matches expected ratio
            if (Math.Abs(profile.ExpectedPixelRatio - screen.DevicePixelRatio) > 0.1f)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ScreenInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: $"Device pixel ratio {screen.DevicePixelRatio} doesn't match typical ratio for {key}",
                    Field: "screen.devicePixelRatio",
                    ExpectedValue: profile.ExpectedPixelRatio.ToString(),
                    ActualValue: screen.DevicePixelRatio.ToString(),
                    SuspiciousnessContribution: 5,
                    Recommendation: $"Use typical device pixel ratio {profile.ExpectedPixelRatio} for {key}"
                ));
            }
        }
        else
        {
            // Check if this is a completely unknown resolution
            var isCommonResolution = CommonDeviceProfiles.Keys.Any(knownKey =>
            {
                var parts = knownKey.Split('x');
                var knownWidth = int.Parse(parts[0]);
                var knownHeight = int.Parse(parts[1]);
                
                // Within 10% of a known resolution
                return Math.Abs(screen.Width - knownWidth) <= knownWidth * 0.1 &&
                       Math.Abs(screen.Height - knownHeight) <= knownHeight * 0.1;
            });

            if (!isCommonResolution)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.StatisticalOutlier,
                    Severity: AnomalySeverity.Low,
                    Description: $"Screen resolution {key} is uncommon or unknown",
                    Field: "screen.dimensions",
                    ExpectedValue: "Common device resolutions",
                    ActualValue: key,
                    SuspiciousnessContribution: 8,
                    Recommendation: "Consider using more common screen resolutions"
                ));
            }
        }
    }

    /// <summary>
    /// Detects temporal inconsistencies in timing-related values
    /// </summary>
    private static void DetectTemporalInconsistencies(Fingerprint fingerprint, List<AnomalyReport> anomalies)
    {
        if (fingerprint.Battery == null) return;

        var battery = fingerprint.Battery;
        
        // Check for inconsistent battery timing
        if (battery.TryGetValue("level", out var levelObj) && levelObj is double level &&
            battery.TryGetValue("charging", out var chargingObj) && chargingObj is bool charging &&
            battery.TryGetValue("chargingTime", out var chargingTimeObj) && chargingTimeObj is double chargingTime &&
            battery.TryGetValue("dischargingTime", out var dischargingTimeObj) && dischargingTimeObj is double dischargingTime)
        {
            if (charging && !double.IsInfinity(chargingTime))
            {
                // Validate charging time makes sense for battery level
                var expectedChargingTime = (1.0 - level) * 7200; // Assume 2 hours for full charge
                if (Math.Abs(chargingTime - expectedChargingTime) > expectedChargingTime * 0.5)
                {
                    anomalies.Add(new AnomalyReport(
                        Type: AnomalyType.SuspiciousTiming,
                        Severity: AnomalySeverity.Low,
                        Description: $"Charging time {chargingTime}s doesn't match battery level {level:P0}",
                        Field: "battery.chargingTime",
                        ExpectedValue: $"~{expectedChargingTime:F0}s for {level:P0} battery",
                        ActualValue: $"{chargingTime}s",
                        SuspiciousnessContribution: 4,
                        Recommendation: "Ensure battery timing values are consistent with battery level"
                    ));
                }
            }

            if (!charging && !double.IsInfinity(dischargingTime))
            {
                // Validate discharging time makes sense for battery level
                var expectedDischargingTime = level * 28800; // Assume 8 hours for full discharge
                if (Math.Abs(dischargingTime - expectedDischargingTime) > expectedDischargingTime * 0.5)
                {
                    anomalies.Add(new AnomalyReport(
                        Type: AnomalyType.SuspiciousTiming,
                        Severity: AnomalySeverity.Low,
                        Description: $"Discharging time {dischargingTime}s doesn't match battery level {level:P0}",
                        Field: "battery.dischargingTime",
                        ExpectedValue: $"~{expectedDischargingTime:F0}s for {level:P0} battery",
                        ActualValue: $"{dischargingTime}s",
                        SuspiciousnessContribution: 4,
                        Recommendation: "Ensure battery timing values are consistent with battery level"
                    ));
                }
            }
        }
    }

    /// <summary>
    /// Represents a known device profile for validation
    /// </summary>
    private record DeviceProfile(
        int Width,
        int Height,
        float ExpectedPixelRatio,
        string DeviceType,
        string[] CompatiblePlatforms
    );
}