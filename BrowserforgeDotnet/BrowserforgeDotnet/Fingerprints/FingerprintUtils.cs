using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using BrowserforgeDotnet.Headers;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Utility functions for fingerprint processing and validation
/// </summary>
public static class FingerprintUtils
{
    /// <summary>
    /// Checks if a screen string representation is within the specified constraints
    /// </summary>
    /// <param name="screenString">Stringified screen dimensions from the network</param>
    /// <param name="screenConstraints">Screen constraint specifications</param>
    /// <returns>True if the screen dimensions satisfy the constraints</returns>
    public static bool IsScreenWithinConstraints(string screenString, Screen screenConstraints)
    {
        if (!screenConstraints.IsSet())
            return true;

        try
        {
            // Remove the *STRINGIFIED* prefix if present
            var jsonString = screenString.StartsWith(FingerprintConstants.StringifiedPrefix)
                ? screenString.Substring(FingerprintConstants.StringifiedPrefix.Length)
                : screenString;

            var screenData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            if (screenData == null)
                return false;

            var width = GetIntValue(screenData, "width", -1);
            var height = GetIntValue(screenData, "height", -1);

            return screenConstraints.SatisfiesConstraints(width, height);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the browser name from a user agent string
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <returns>Browser name or null if not detected</returns>
    public static string? GetBrowserFromUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return null;

        // Chrome/Chromium detection (must be before Safari)
        if (userAgent.Contains("Chrome/") || userAgent.Contains("Chromium/"))
        {
            if (userAgent.Contains("Edg/"))
                return "edge";
            if (userAgent.Contains("OPR/") || userAgent.Contains("Opera/"))
                return "opera";
            return "chrome";
        }

        // Firefox detection
        if (userAgent.Contains("Firefox/"))
            return "firefox";

        // Safari detection (must be after Chrome detection)
        if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/"))
            return "safari";

        // Internet Explorer detection
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident/"))
            return "ie";

        return null;
    }

    /// <summary>
    /// Generates accept-language header value from locale list
    /// </summary>
    /// <param name="locales">List of supported locales</param>
    /// <returns>Accept-Language header value</returns>
    public static string GenerateAcceptLanguageHeader(IEnumerable<string> locales)
    {
        var localeList = locales.ToList();
        if (!localeList.Any())
            return "en-US,en;q=0.9";

        var parts = new List<string>();
        var quality = 1.0;

        foreach (var locale in localeList.Take(10)) // Limit to 10 locales
        {
            if (quality >= 1.0)
            {
                parts.Add(locale);
            }
            else
            {
                parts.Add($"{locale};q={quality.ToString("F1", CultureInfo.InvariantCulture)}");
            }
            quality = Math.Max(0.1, quality - 0.1);
        }

        return string.Join(",", parts);
    }

    /// <summary>
    /// Determines if Sec-Fetch headers should be added based on the browser
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <returns>True if Sec-Fetch headers should be added</returns>
    public static bool ShouldAddSecFetchHeaders(string userAgent)
    {
        var browser = GetBrowserFromUserAgent(userAgent);
        return browser == "chrome" || browser == "edge";
    }

    /// <summary>
    /// Gets Sec-Fetch headers for the specified HTTP version
    /// </summary>
    /// <param name="httpVersion">HTTP version ("1" or "2")</param>
    /// <returns>Dictionary of Sec-Fetch headers</returns>
    public static Dictionary<string, string> GetSecFetchHeaders(string httpVersion = "1")
    {
        var key = httpVersion == "2" ? "http2" : "http1";
        return FingerprintConstants.SecFetchAttributes.TryGetValue(key, out var headers)
            ? new Dictionary<string, string>(headers)
            : new Dictionary<string, string>();
    }

    /// <summary>
    /// Generates realistic battery information
    /// </summary>
    /// <param name="charging">Whether the battery is charging</param>
    /// <returns>Dictionary containing battery information</returns>
    public static Dictionary<string, object> GenerateBatteryInfo(bool? charging = null)
    {
        var isCharging = charging ?? Random.Shared.NextDouble() > 0.3; // 70% chance of charging
        var level = isCharging 
            ? Random.Shared.NextDouble() * 0.5 + 0.5 // 50-100% when charging
            : Random.Shared.NextDouble(); // 0-100% when not charging

        return new Dictionary<string, object>
        {
            { "charging", isCharging },
            { "chargingTime", isCharging ? Random.Shared.Next(1800, 7200) : double.PositiveInfinity },
            { "dischargingTime", isCharging ? double.PositiveInfinity : Random.Shared.Next(3600, 28800) },
            { "level", Math.Round(level, 2) }
        };
    }

    /// <summary>
    /// Filters and randomizes font list to simulate realistic font detection
    /// </summary>
    /// <param name="availableFonts">Available font list</param>
    /// <param name="platform">Operating system platform</param>
    /// <returns>Filtered font list appropriate for the platform</returns>
    public static List<string> FilterFontsForPlatform(List<string> availableFonts, string platform)
    {
        var platformFonts = platform.ToLowerInvariant() switch
        {
            var p when p.Contains("win") => GetWindowsFonts(availableFonts),
            var p when p.Contains("mac") => GetMacFonts(availableFonts),
            var p when p.Contains("linux") => GetLinuxFonts(availableFonts),
            _ => availableFonts
        };

        // Randomize the order and potentially remove some fonts to simulate realistic variation
        var shuffled = platformFonts.OrderBy(_ => Random.Shared.Next()).ToList();
        var removeCount = Random.Shared.Next(0, Math.Min(5, shuffled.Count / 4));
        
        return shuffled.Skip(removeCount).ToList();
    }

    /// <summary>
    /// Generates multimedia device list based on platform
    /// </summary>
    /// <param name="platform">Operating system platform</param>
    /// <returns>List of multimedia device names</returns>
    public static List<string> GenerateMultimediaDevices(string platform)
    {
        var baseDevices = platform.ToLowerInvariant() switch
        {
            var p when p.Contains("win") => new List<string>
            {
                "Default - Microphone (Realtek Audio)",
                "Default - Speakers (Realtek Audio)",
                "Microphone (Realtek Audio)",
                "Speakers (Realtek Audio)"
            },
            var p when p.Contains("mac") => new List<string>
            {
                "Built-in Microphone",
                "Built-in Output",
                "MacBook Pro Microphone",
                "MacBook Pro Speakers"
            },
            _ => new List<string>
            {
                "Default Audio Device",
                "Built-in Audio Analog Stereo"
            }
        };

        // Add some randomization
        if (Random.Shared.NextDouble() > 0.7)
        {
            baseDevices.Add("USB Headset");
        }

        return baseDevices;
    }

    /// <summary>
    /// Validates that a fingerprint has all required components
    /// </summary>
    /// <param name="fingerprint">Fingerprint to validate</param>
    /// <returns>True if the fingerprint is valid</returns>
    public static bool ValidateFingerprint(Fingerprint fingerprint)
    {
        if (fingerprint.Screen == null || fingerprint.Navigator == null)
            return false;

        if (string.IsNullOrEmpty(fingerprint.Navigator.UserAgent))
            return false;

        if (fingerprint.Screen.Width <= 0 || fingerprint.Screen.Height <= 0)
            return false;

        if (!fingerprint.Headers.ContainsKey("User-Agent") && !fingerprint.Headers.ContainsKey("user-agent"))
            return false;

        return true;
    }

    private static int GetIntValue(Dictionary<string, object> data, string key, int defaultValue)
    {
        if (!data.TryGetValue(key, out var value))
            return defaultValue;

        return value switch
        {
            int intVal => intVal,
            long longVal => (int)longVal,
            double doubleVal => (int)doubleVal,
            float floatVal => (int)floatVal,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number => jsonElement.GetInt32(),
            string strVal when int.TryParse(strVal, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static List<string> GetWindowsFonts(List<string> availableFonts)
    {
        var windowsFonts = new[]
        {
            "Arial", "Arial Black", "Calibri", "Cambria", "Cambria Math", "Comic Sans MS",
            "Consolas", "Courier New", "Georgia", "Impact", "Lucida Console",
            "Lucida Sans Unicode", "Microsoft Sans Serif", "Palatino Linotype",
            "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"
        };

        return availableFonts.Where(font => windowsFonts.Contains(font, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static List<string> GetMacFonts(List<string> availableFonts)
    {
        var macFonts = new[]
        {
            "Arial", "Arial Black", "Helvetica", "Helvetica Neue", "Times", "Times New Roman",
            "Courier", "Courier New", "Georgia", "Verdana", "Monaco", "Menlo",
            "San Francisco", "Lucida Grande", "Apple Symbols"
        };

        return availableFonts.Where(font => macFonts.Contains(font, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static List<string> GetLinuxFonts(List<string> availableFonts)
    {
        var linuxFonts = new[]
        {
            "Arial", "Helvetica", "Times", "Times New Roman", "Courier", "Courier New",
            "Georgia", "Verdana", "Ubuntu", "Liberation Sans", "Liberation Serif",
            "Liberation Mono", "DejaVu Sans", "DejaVu Serif", "DejaVu Sans Mono"
        };

        return availableFonts.Where(font => linuxFonts.Contains(font, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Detects anomalies in the provided fingerprint using advanced anomaly detection
    /// </summary>
    /// <param name="fingerprint">Fingerprint to analyze for anomalies</param>
    /// <param name="referenceDataset">Optional reference dataset for comparison</param>
    /// <returns>List of detected anomaly reports</returns>
    public static List<AnomalyReport> DetectAnomalies(Fingerprint fingerprint, List<Fingerprint>? referenceDataset = null)
    {
        return FingerprintAnomalyDetector.DetectAnomalies(fingerprint, referenceDataset);
    }

    /// <summary>
    /// Calculates a suspiciousness score for the fingerprint based on various risk factors
    /// </summary>
    /// <param name="fingerprint">Fingerprint to analyze</param>
    /// <returns>Suspiciousness score breakdown with detailed analysis</returns>
    public static SuspiciousnessScore GetSuspiciousnessScore(Fingerprint fingerprint)
    {
        // Get validation result which includes anomaly detection
        var validationResult = FingerprintValidator.ValidateFingerprint(fingerprint);
        var anomalies = validationResult.Anomalies;

        // Calculate component scores
        var componentScores = new Dictionary<string, int>
        {
            ["browserPlatformConsistency"] = CalculateComponentScore(anomalies, AnomalyType.BrowserPlatformInconsistency),
            ["hardwareRealism"] = CalculateComponentScore(anomalies, AnomalyType.UnrealisticHardware, AnomalyType.ImpossibleHardware),
            ["screenConsistency"] = CalculateComponentScore(anomalies, AnomalyType.ScreenInconsistency),
            ["languageConsistency"] = CalculateComponentScore(anomalies, AnomalyType.LanguageInconsistency),
            ["codecConsistency"] = CalculateComponentScore(anomalies, AnomalyType.CodecInconsistency),
            ["navigatorConsistency"] = CalculateComponentScore(anomalies, AnomalyType.NavigatorInconsistency),
            ["fontConsistency"] = CalculateComponentScore(anomalies, AnomalyType.FontInconsistency),
            ["batteryRealism"] = CalculateComponentScore(anomalies, AnomalyType.UnrealisticBattery),
            ["multimediaConsistency"] = CalculateComponentScore(anomalies, AnomalyType.MultimediaInconsistency),
            ["statisticalOutliers"] = CalculateComponentScore(anomalies, AnomalyType.StatisticalOutlier),
            ["automatedGeneration"] = CalculateComponentScore(anomalies, AnomalyType.AutomatedGeneration),
            ["commonPatterns"] = CalculateComponentScore(anomalies, AnomalyType.TooCommon)
        };

        // Identify risk factors
        var riskFactors = new List<string>();
        foreach (var anomaly in anomalies.Where(a => a.Severity >= AnomalySeverity.Medium))
        {
            riskFactors.Add($"{anomaly.Type}: {anomaly.Description}");
        }

        // Generate recommendations
        var recommendations = GenerateRecommendations(anomalies);

        return new SuspiciousnessScore(
            OverallScore: validationResult.SuspiciousnessScore,
            ComponentScores: componentScores,
            RiskFactors: riskFactors,
            Recommendations: recommendations
        );
    }

    /// <summary>
    /// Validates hardware consistency across all hardware-related components
    /// </summary>
    /// <param name="fingerprint">Fingerprint to validate</param>
    /// <returns>Detailed hardware validation result</returns>
    public static ValidationResult ValidateHardwareConsistency(Fingerprint fingerprint)
    {
        var validations = new List<ValidationCheck>();
        var anomalies = new List<AnomalyReport>();
        var details = new Dictionary<string, object>();

        // Validate CPU-Memory relationship
        if (fingerprint.Navigator.DeviceMemory.HasValue)
        {
            var memory = fingerprint.Navigator.DeviceMemory.Value;
            var cores = fingerprint.Navigator.HardwareConcurrency;
            var ratio = (double)memory / cores;
            
            var cpuMemoryValid = ratio >= 0.5 && ratio <= 8.0;
            validations.Add(new ValidationCheck(
                "CPU-Memory Ratio",
                cpuMemoryValid,
                "Validates memory and CPU core count are proportional",
                "0.5-8.0 GB per core",
                $"{ratio:F1} GB per core",
                cpuMemoryValid ? AnomalySeverity.Low : AnomalySeverity.Medium
            ));

            if (!cpuMemoryValid)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ImpossibleHardware,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Memory-to-CPU ratio {ratio:F1} is unrealistic",
                    Field: "hardware_ratio",
                    ExpectedValue: "0.5-8.0 GB per core",
                    ActualValue: $"{ratio:F1} GB per core",
                    SuspiciousnessContribution: 15,
                    Recommendation: "Ensure memory and CPU specifications are proportional"
                ));
            }
        }

        // Validate Screen-GPU relationship
        var screenPixels = fingerprint.Screen.Width * fingerprint.Screen.Height;
        var isHighRes = screenPixels > 2073600; // Above 1920x1080
        
        details["hardwareAnalysis"] = new
        {
            screenResolution = $"{fingerprint.Screen.Width}x{fingerprint.Screen.Height}",
            totalPixels = screenPixels,
            isHighResolution = isHighRes,
            deviceMemory = fingerprint.Navigator.DeviceMemory,
            cpuCores = fingerprint.Navigator.HardwareConcurrency,
            hasVideoCard = fingerprint.VideoCard != null
        };

        // High resolution displays should have sufficient hardware
        if (isHighRes && fingerprint.Navigator.DeviceMemory.HasValue && fingerprint.Navigator.DeviceMemory.Value < 4)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ImpossibleHardware,
                Severity: AnomalySeverity.Medium,
                Description: "High resolution display with insufficient memory",
                Field: "screen_memory_mismatch",
                ExpectedValue: "At least 4GB for high-res displays",
                ActualValue: $"{fingerprint.Navigator.DeviceMemory}GB",
                SuspiciousnessContribution: 10,
                Recommendation: "Increase device memory for high resolution displays"
            ));
        }

        var overallValid = anomalies.Count == 0;
        var suspiciousnessScore = anomalies.Sum(a => a.SuspiciousnessContribution);

        return new ValidationResult(overallValid, suspiciousnessScore, anomalies, validations, details);
    }

    /// <summary>
    /// Checks browser fingerprint realism by analyzing consistency and plausibility
    /// </summary>
    /// <param name="fingerprint">Fingerprint to assess</param>
    /// <returns>Realism assessment with detailed analysis</returns>
    public static ValidationResult CheckBrowserFingerprintRealism(Fingerprint fingerprint)
    {
        var validations = new List<ValidationCheck>();
        var anomalies = new List<AnomalyReport>();
        var details = new Dictionary<string, object>();

        // Perform comprehensive validation
        var mainValidation = FingerprintValidator.ValidateFingerprint(fingerprint);
        var additionalAnomalies = FingerprintAnomalyDetector.DetectAnomalies(fingerprint);

        // Combine results
        anomalies.AddRange(mainValidation.Anomalies);
        anomalies.AddRange(additionalAnomalies.Where(a => !mainValidation.Anomalies.Any(existing =>
            existing.Type == a.Type && existing.Field == a.Field)));
        validations.AddRange(mainValidation.Validations);

        // Calculate realism metrics
        var totalAnomalies = anomalies.Count;
        var criticalAnomalies = anomalies.Count(a => a.Severity == AnomalySeverity.Critical);
        var highAnomalies = anomalies.Count(a => a.Severity == AnomalySeverity.High);
        var mediumAnomalies = anomalies.Count(a => a.Severity == AnomalySeverity.Medium);
        var lowAnomalies = anomalies.Count(a => a.Severity == AnomalySeverity.Low);

        // Determine realism level
        var realismLevel = (criticalAnomalies, highAnomalies, mediumAnomalies) switch
        {
            (> 0, _, _) => "Very Low - Critical issues detected",
            (0, > 2, _) => "Low - Multiple high-severity issues",
            (0, > 0, > 3) => "Medium-Low - Some significant issues",
            (0, 0, > 2) => "Medium - Moderate issues present",
            (0, 0, <= 2) when lowAnomalies > 5 => "Medium-High - Many minor issues",
            (0, 0, <= 2) when lowAnomalies <= 3 => "High - Appears realistic",
            _ => "Very High - Highly realistic fingerprint"
        };

        details["realismAssessment"] = new
        {
            realismLevel,
            totalAnomalies,
            anomaliesBySeverity = new
            {
                critical = criticalAnomalies,
                high = highAnomalies,
                medium = mediumAnomalies,
                low = lowAnomalies
            },
            riskCategories = anomalies.GroupBy(a => a.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            detectionRisk = mainValidation.SuspiciousnessScore switch
            {
                >= 80 => "Very High",
                >= 60 => "High",
                >= 40 => "Medium",
                >= 20 => "Low",
                _ => "Very Low"
            }
        };

        var isRealistic = criticalAnomalies == 0 && highAnomalies <= 1 && mainValidation.SuspiciousnessScore < 60;

        return new ValidationResult(
            IsValid: isRealistic,
            SuspiciousnessScore: mainValidation.SuspiciousnessScore,
            Anomalies: anomalies,
            Validations: validations,
            Details: details
        );
    }

    /// <summary>
    /// Calculates component score based on anomalies of specific types
    /// </summary>
    private static int CalculateComponentScore(List<AnomalyReport> anomalies, params AnomalyType[] types)
    {
        var relevantAnomalies = anomalies.Where(a => types.Contains(a.Type));
        return relevantAnomalies.Sum(a => a.SuspiciousnessContribution);
    }

    /// <summary>
    /// Generates recommendations based on detected anomalies
    /// </summary>
    private static List<string> GenerateRecommendations(List<AnomalyReport> anomalies)
    {
        var recommendations = new List<string>();

        // Group anomalies by type and generate specific recommendations
        var anomalyGroups = anomalies.GroupBy(a => a.Type);

        foreach (var group in anomalyGroups)
        {
            var recommendation = group.Key switch
            {
                AnomalyType.BrowserPlatformInconsistency => "Ensure browser and platform combinations are realistic (e.g., Safari only on macOS/iOS)",
                AnomalyType.UnrealisticHardware => "Use realistic hardware specifications within normal ranges",
                AnomalyType.ScreenInconsistency => "Validate screen dimensions, aspect ratios, and device pixel ratios are consistent",
                AnomalyType.LanguageInconsistency => "Ensure language settings are consistent across navigator properties and headers",
                AnomalyType.NavigatorInconsistency => "Match navigator properties with User-Agent string and avoid webdriver indicators",
                AnomalyType.StatisticalOutlier => "Use more common hardware configurations to avoid statistical detection",
                AnomalyType.AutomatedGeneration => "Add natural variation to avoid patterns that suggest automated generation",
                AnomalyType.TooCommon => "Introduce slight variations to avoid overly common fingerprint patterns",
                _ => "Address detected inconsistencies to improve fingerprint realism"
            };

            if (!recommendations.Contains(recommendation))
            {
                recommendations.Add(recommendation);
            }
        }

        // Add general recommendations based on severity
        var hasCritical = anomalies.Any(a => a.Severity == AnomalySeverity.Critical);
        var hasHigh = anomalies.Any(a => a.Severity == AnomalySeverity.High);

        if (hasCritical)
        {
            recommendations.Insert(0, "URGENT: Address critical anomalies immediately - fingerprint is highly detectable");
        }
        else if (hasHigh)
        {
            recommendations.Insert(0, "Address high-severity issues to significantly improve fingerprint quality");
        }

        return recommendations;
    }
}