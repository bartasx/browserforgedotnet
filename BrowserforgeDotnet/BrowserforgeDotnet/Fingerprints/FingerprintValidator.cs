using System.Text.RegularExpressions;
using System.Globalization;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Advanced fingerprint validation system that checks for suspicious or unrealistic fingerprints
/// </summary>
public static class FingerprintValidator
{
    private static readonly Dictionary<string, string[]> BrowserPlatformCompatibility = new()
    {
        { "safari", new[] { "MacIntel", "iPhone", "iPad" } },
        { "chrome", new[] { "Win32", "MacIntel", "Linux x86_64", "Linux i686" } },
        { "firefox", new[] { "Win32", "MacIntel", "Linux x86_64", "Linux i686" } },
        { "edge", new[] { "Win32", "MacIntel" } },
        { "opera", new[] { "Win32", "MacIntel", "Linux x86_64", "Linux i686" } }
    };

    private static readonly Dictionary<string, (int min, int max)> HardwareRanges = new()
    {
        { "deviceMemory", (2, 32) },
        { "hardwareConcurrency", (1, 32) },
        { "screenWidth", (320, 7680) },
        { "screenHeight", (240, 4320) },
        { "colorDepth", (16, 32) },
        { "pixelDepth", (16, 32) }
    };

    private static readonly Dictionary<string, float[]> CommonDevicePixelRatios = new()
    {
        { "default", new[] { 1.0f, 1.25f, 1.5f, 2.0f, 2.25f, 3.0f } },
        { "mac", new[] { 1.0f, 2.0f } },
        { "mobile", new[] { 1.5f, 2.0f, 2.25f, 3.0f, 4.0f } }
    };

    /// <summary>
    /// Performs comprehensive validation of a fingerprint for suspicious or unrealistic characteristics
    /// </summary>
    /// <param name="fingerprint">Fingerprint to validate</param>
    /// <returns>Detailed validation result</returns>
    public static ValidationResult ValidateFingerprint(Fingerprint fingerprint)
    {
        var validations = new List<ValidationCheck>();
        var anomalies = new List<AnomalyReport>();
        var details = new Dictionary<string, object>();

        // Perform all validation checks
        ValidateBrowserPlatformConsistency(fingerprint, validations, anomalies);
        ValidateHardwareRealism(fingerprint, validations, anomalies);
        ValidateScreenConsistency(fingerprint, validations, anomalies);
        ValidateLanguageConsistency(fingerprint, validations, anomalies);
        ValidateCodecConsistency(fingerprint, validations, anomalies);
        ValidateNavigatorConsistency(fingerprint, validations, anomalies);
        ValidateFontConsistency(fingerprint, validations, anomalies);
        ValidateBatteryRealism(fingerprint, validations, anomalies);
        ValidateMultimediaConsistency(fingerprint, validations, anomalies);

        // Calculate overall suspiciousness score
        var suspiciousnessScore = CalculateSuspiciousnessScore(anomalies);
        
        // Add validation summary to details
        details["validationSummary"] = new
        {
            totalChecks = validations.Count,
            passedChecks = validations.Count(v => v.Passed),
            failedChecks = validations.Count(v => !v.Passed),
            anomaliesFound = anomalies.Count,
            criticalAnomalies = anomalies.Count(a => a.Severity == AnomalySeverity.Critical),
            highAnomalies = anomalies.Count(a => a.Severity == AnomalySeverity.High)
        };

        var isValid = anomalies.Count == 0 || !anomalies.Any(a => a.Severity >= AnomalySeverity.High);

        return new ValidationResult(isValid, suspiciousnessScore, anomalies, validations, details);
    }

    /// <summary>
    /// Validates that browser and platform combination is realistic
    /// </summary>
    private static void ValidateBrowserPlatformConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var browser = FingerprintUtils.GetBrowserFromUserAgent(fingerprint.Navigator.UserAgent);
        var platform = fingerprint.Navigator.Platform;

        var checkName = "Browser-Platform Consistency";
        var passed = true;

        if (browser != null && BrowserPlatformCompatibility.TryGetValue(browser, out var compatiblePlatforms))
        {
            if (!compatiblePlatforms.Contains(platform))
            {
                passed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.BrowserPlatformInconsistency,
                    Severity: AnomalySeverity.High,
                    Description: $"Browser '{browser}' is not typically found on platform '{platform}'",
                    Field: "navigator.platform",
                    ExpectedValue: string.Join(", ", compatiblePlatforms),
                    ActualValue: platform,
                    SuspiciousnessContribution: 25,
                    Recommendation: $"Use a platform compatible with {browser}: {string.Join(", ", compatiblePlatforms)}"
                ));
            }
        }

        // Special case: Safari should only be on Mac/iOS
        if (browser == "safari" && !platform.Contains("Mac") && !platform.Contains("iPhone") && !platform.Contains("iPad"))
        {
            passed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.BrowserPlatformInconsistency,
                Severity: AnomalySeverity.Critical,
                Description: "Safari browser detected on non-Apple platform",
                Field: "navigator.platform",
                ExpectedValue: "MacIntel, iPhone, or iPad",
                ActualValue: platform,
                SuspiciousnessContribution: 40,
                Recommendation: "Safari should only be used on Apple platforms"
            ));
        }

        validations.Add(new ValidationCheck(checkName, passed, "Validates browser and platform compatibility", 
            browser != null ? string.Join(", ", BrowserPlatformCompatibility.GetValueOrDefault(browser, new[] { "any" })) : "any", platform, 
            passed ? AnomalySeverity.Low : AnomalySeverity.High));
    }

    /// <summary>
    /// Validates hardware values are within realistic ranges and combinations
    /// </summary>
    private static void ValidateHardwareRealism(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        // Validate device memory
        if (fingerprint.Navigator.DeviceMemory.HasValue)
        {
            var memory = fingerprint.Navigator.DeviceMemory.Value;
            var (minMem, maxMem) = HardwareRanges["deviceMemory"];
            var memoryCheckPassed = memory >= minMem && memory <= maxMem;

            if (!memoryCheckPassed)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.UnrealisticHardware,
                    Severity: memory < minMem || memory > 64 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description: $"Device memory {memory}GB is outside realistic range",
                    Field: "navigator.deviceMemory",
                    ExpectedValue: $"{minMem}-{maxMem}GB",
                    ActualValue: $"{memory}GB",
                    SuspiciousnessContribution: 15,
                    Recommendation: $"Use device memory between {minMem}GB and {maxMem}GB"
                ));
            }

            validations.Add(new ValidationCheck("Device Memory Range", memoryCheckPassed, "Validates device memory is within realistic range",
                $"{minMem}-{maxMem}GB", $"{memory}GB", memoryCheckPassed ? AnomalySeverity.Low : AnomalySeverity.Medium));
        }

        // Validate hardware concurrency
        var cores = fingerprint.Navigator.HardwareConcurrency;
        var (minCores, maxCores) = HardwareRanges["hardwareConcurrency"];
        var coresCheckPassed = cores >= minCores && cores <= maxCores;

        if (!coresCheckPassed)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.UnrealisticHardware,
                Severity: cores > 64 ? AnomalySeverity.High : AnomalySeverity.Medium,
                Description: $"Hardware concurrency {cores} is outside realistic range",
                Field: "navigator.hardwareConcurrency",
                ExpectedValue: $"{minCores}-{maxCores}",
                ActualValue: cores.ToString(),
                SuspiciousnessContribution: 10,
                Recommendation: $"Use hardware concurrency between {minCores} and {maxCores}"
            ));
        }

        validations.Add(new ValidationCheck("Hardware Concurrency Range", coresCheckPassed, "Validates CPU core count is realistic",
            $"{minCores}-{maxCores}", cores.ToString(), coresCheckPassed ? AnomalySeverity.Low : AnomalySeverity.Medium));

        // Validate memory-to-CPU ratio
        if (fingerprint.Navigator.DeviceMemory.HasValue)
        {
            var memoryToCpuRatio = (double)fingerprint.Navigator.DeviceMemory.Value / cores;
            var ratioCheckPassed = memoryToCpuRatio >= 0.5 && memoryToCpuRatio <= 8.0;

            if (!ratioCheckPassed)
            {
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.ImpossibleHardware,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Memory-to-CPU ratio {memoryToCpuRatio:F1} is unrealistic",
                    Field: "hardware_ratio",
                    ExpectedValue: "0.5-8.0 GB per core",
                    ActualValue: $"{memoryToCpuRatio:F1} GB per core",
                    SuspiciousnessContribution: 12,
                    Recommendation: "Ensure memory and CPU counts are proportional"
                ));
            }

            validations.Add(new ValidationCheck("Memory-CPU Ratio", ratioCheckPassed, "Validates memory and CPU are proportional",
                "0.5-8.0 GB per core", $"{memoryToCpuRatio:F1} GB per core", ratioCheckPassed ? AnomalySeverity.Low : AnomalySeverity.Medium));
        }
    }

    /// <summary>
    /// Validates screen dimensions and device pixel ratio correlations
    /// </summary>
    private static void ValidateScreenConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var screen = fingerprint.Screen;
        
        // Validate screen dimensions
        var (minWidth, maxWidth) = HardwareRanges["screenWidth"];
        var (minHeight, maxHeight) = HardwareRanges["screenHeight"];
        
        var widthCheckPassed = screen.Width >= minWidth && screen.Width <= maxWidth;
        var heightCheckPassed = screen.Height >= minHeight && screen.Height <= maxHeight;

        if (!widthCheckPassed)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ScreenInconsistency,
                Severity: AnomalySeverity.Medium,
                Description: $"Screen width {screen.Width} is outside realistic range",
                Field: "screen.width",
                ExpectedValue: $"{minWidth}-{maxWidth}",
                ActualValue: screen.Width.ToString(),
                SuspiciousnessContribution: 8,
                Recommendation: $"Use screen width between {minWidth} and {maxWidth}"
            ));
        }

        if (!heightCheckPassed)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ScreenInconsistency,
                Severity: AnomalySeverity.Medium,
                Description: $"Screen height {screen.Height} is outside realistic range",
                Field: "screen.height",
                ExpectedValue: $"{minHeight}-{maxHeight}",
                ActualValue: screen.Height.ToString(),
                SuspiciousnessContribution: 8,
                Recommendation: $"Use screen height between {minHeight} and {maxHeight}"
            ));
        }

        // Validate device pixel ratio
        var platform = fingerprint.Navigator.Platform.ToLowerInvariant();
        var expectedRatios = platform.Contains("mac") ? CommonDevicePixelRatios["mac"] : CommonDevicePixelRatios["default"];
        var ratioCheckPassed = expectedRatios.Any(r => Math.Abs(r - screen.DevicePixelRatio) < 0.01f);

        if (!ratioCheckPassed)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ScreenInconsistency,
                Severity: AnomalySeverity.Low,
                Description: $"Device pixel ratio {screen.DevicePixelRatio} is uncommon for this platform",
                Field: "screen.devicePixelRatio",
                ExpectedValue: string.Join(", ", expectedRatios),
                ActualValue: screen.DevicePixelRatio.ToString(CultureInfo.InvariantCulture),
                SuspiciousnessContribution: 5,
                Recommendation: $"Use common device pixel ratios: {string.Join(", ", expectedRatios)}"
            ));
        }

        // Validate screen dimension relationships
        var aspectRatio = (double)screen.Width / screen.Height;
        var commonAspectRatios = new[] { 16.0/9.0, 16.0/10.0, 4.0/3.0, 21.0/9.0, 32.0/9.0 };
        var aspectRatioCheckPassed = commonAspectRatios.Any(ar => Math.Abs(ar - aspectRatio) < 0.1);

        if (!aspectRatioCheckPassed)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ScreenInconsistency,
                Severity: AnomalySeverity.Low,
                Description: $"Screen aspect ratio {aspectRatio:F2} is uncommon",
                Field: "screen_aspect_ratio",
                ExpectedValue: "16:9, 16:10, 4:3, 21:9, or 32:9",
                ActualValue: $"{aspectRatio:F2}:1",
                SuspiciousnessContribution: 3,
                Recommendation: "Use common screen aspect ratios"
            ));
        }

        // Validate available vs total dimensions
        var availWidthCheck = screen.AvailWidth <= screen.Width && screen.AvailWidth >= screen.Width - 100;
        var availHeightCheck = screen.AvailHeight <= screen.Height && screen.AvailHeight >= screen.Height - 200;

        if (!availWidthCheck || !availHeightCheck)
        {
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.ScreenInconsistency,
                Severity: AnomalySeverity.Medium,
                Description: "Available screen dimensions are inconsistent with total dimensions",
                Field: "screen.available_dimensions",
                ExpectedValue: "Available dimensions should be slightly less than total",
                ActualValue: $"Avail: {screen.AvailWidth}x{screen.AvailHeight}, Total: {screen.Width}x{screen.Height}",
                SuspiciousnessContribution: 10,
                Recommendation: "Ensure available dimensions account for taskbars and UI elements"
            ));
        }

        validations.Add(new ValidationCheck("Screen Dimensions", widthCheckPassed && heightCheckPassed, 
            "Validates screen dimensions are realistic", $"{minWidth}x{minHeight} to {maxWidth}x{maxHeight}", 
            $"{screen.Width}x{screen.Height}", (widthCheckPassed && heightCheckPassed) ? AnomalySeverity.Low : AnomalySeverity.Medium));
            
        validations.Add(new ValidationCheck("Device Pixel Ratio", ratioCheckPassed, 
            "Validates device pixel ratio is common for platform", string.Join(", ", expectedRatios), 
            screen.DevicePixelRatio.ToString(CultureInfo.InvariantCulture), ratioCheckPassed ? AnomalySeverity.Low : AnomalySeverity.Low));
            
        validations.Add(new ValidationCheck("Screen Available Dimensions", availWidthCheck && availHeightCheck, 
            "Validates available screen dimensions are consistent", "Slightly less than total dimensions", 
            $"Avail: {screen.AvailWidth}x{screen.AvailHeight}", (availWidthCheck && availHeightCheck) ? AnomalySeverity.Low : AnomalySeverity.Medium));
    }

    /// <summary>
    /// Validates language settings consistency with platform and headers
    /// </summary>
    private static void ValidateLanguageConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var navigatorLanguage = fingerprint.Navigator.Language;
        var navigatorLanguages = fingerprint.Navigator.Languages;
        
        // Extract Accept-Language from headers
        var acceptLanguage = fingerprint.Headers.FirstOrDefault(h => 
            h.Key.Equals("Accept-Language", StringComparison.OrdinalIgnoreCase)).Value;

        var languageConsistencyPassed = true;

        // Check if navigator.language is first in navigator.languages
        if (navigatorLanguages.Count > 0 && navigatorLanguages[0] != navigatorLanguage)
        {
            languageConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.LanguageInconsistency,
                Severity: AnomalySeverity.Medium,
                Description: "Navigator language doesn't match first language in languages array",
                Field: "navigator.language",
                ExpectedValue: navigatorLanguages[0],
                ActualValue: navigatorLanguage,
                SuspiciousnessContribution: 8,
                Recommendation: "Ensure navigator.language matches first element in navigator.languages"
            ));
        }

        // Check Accept-Language header consistency
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var acceptLangFirst = acceptLanguage.Split(',')[0].Split(';')[0].Trim();
            if (acceptLangFirst != navigatorLanguage)
            {
                languageConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.LanguageInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: "Accept-Language header doesn't match navigator.language",
                    Field: "headers.accept-language",
                    ExpectedValue: navigatorLanguage,
                    ActualValue: acceptLangFirst,
                    SuspiciousnessContribution: 5,
                    Recommendation: "Ensure Accept-Language header starts with navigator.language"
                ));
            }
        }

        // Validate language format
        var languageFormatRegex = new Regex(@"^[a-z]{2}(-[A-Z]{2})?$");
        if (!languageFormatRegex.IsMatch(navigatorLanguage))
        {
            languageConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.LanguageInconsistency,
                Severity: AnomalySeverity.Low,
                Description: "Navigator language format is invalid",
                Field: "navigator.language",
                ExpectedValue: "Valid language code (e.g., 'en-US', 'fr', 'de-DE')",
                ActualValue: navigatorLanguage,
                SuspiciousnessContribution: 3,
                Recommendation: "Use valid language codes in ISO 639-1 format"
            ));
        }

        validations.Add(new ValidationCheck("Language Consistency", languageConsistencyPassed, 
            "Validates language settings are consistent across navigator and headers", 
            "Consistent language settings", $"Language: {navigatorLanguage}, Accept-Language: {acceptLanguage}", 
            languageConsistencyPassed ? AnomalySeverity.Low : AnomalySeverity.Medium));
    }

    /// <summary>
    /// Validates codec support consistency with browser type
    /// </summary>
    private static void ValidateCodecConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var browser = FingerprintUtils.GetBrowserFromUserAgent(fingerprint.Navigator.UserAgent);
        var videoCodecs = fingerprint.VideoCodecs;
        var audioCodecs = fingerprint.AudioCodecs;

        var codecConsistencyPassed = true;

        // Chrome/Edge should support VP8, VP9, H.264
        if (browser == "chrome" || browser == "edge")
        {
            var expectedVideoCodecs = new[] { "video/mp4", "video/webm" };
            var expectedAudioCodecs = new[] { "audio/mpeg", "audio/ogg", "audio/wav", "audio/webm" };

            foreach (var expectedCodec in expectedVideoCodecs)
            {
                if (!videoCodecs.ContainsKey(expectedCodec))
                {
                    codecConsistencyPassed = false;
                    anomalies.Add(new AnomalyReport(
                        Type: AnomalyType.CodecInconsistency,
                        Severity: AnomalySeverity.Low,
                        Description: $"{browser} typically supports {expectedCodec} codec",
                        Field: "videoCodecs",
                        ExpectedValue: expectedCodec,
                        ActualValue: "Not present",
                        SuspiciousnessContribution: 3,
                        Recommendation: $"Add {expectedCodec} codec support for {browser}"
                    ));
                }
            }
        }

        // Firefox should support Ogg codecs
        if (browser == "firefox")
        {
            if (!audioCodecs.ContainsKey("audio/ogg"))
            {
                codecConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.CodecInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: "Firefox typically supports Ogg audio codec",
                    Field: "audioCodecs",
                    ExpectedValue: "audio/ogg",
                    ActualValue: "Not present",
                    SuspiciousnessContribution: 4,
                    Recommendation: "Add Ogg codec support for Firefox"
                ));
            }
        }

        // Safari has specific codec limitations
        if (browser == "safari")
        {
            if (videoCodecs.ContainsKey("video/webm"))
            {
                codecConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.CodecInconsistency,
                    Severity: AnomalySeverity.Medium,
                    Description: "Safari typically doesn't support WebM video codec",
                    Field: "videoCodecs",
                    ExpectedValue: "Should not contain video/webm",
                    ActualValue: "Contains video/webm",
                    SuspiciousnessContribution: 8,
                    Recommendation: "Remove WebM codec support for Safari"
                ));
            }
        }

        validations.Add(new ValidationCheck("Codec Consistency", codecConsistencyPassed, 
            "Validates codec support matches browser capabilities", 
            $"Codecs appropriate for {browser ?? "unknown browser"}", 
            $"Video: {videoCodecs.Count} codecs, Audio: {audioCodecs.Count} codecs", 
            codecConsistencyPassed ? AnomalySeverity.Low : AnomalySeverity.Low));
    }

    /// <summary>
    /// Validates navigator properties match User-Agent string
    /// </summary>
    private static void ValidateNavigatorConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var navigator = fingerprint.Navigator;
        var userAgent = navigator.UserAgent;
        var navigatorConsistencyPassed = true;

        // Validate platform consistency with User-Agent
        if (userAgent.Contains("Windows") && !navigator.Platform.Contains("Win"))
        {
            navigatorConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.NavigatorInconsistency,
                Severity: AnomalySeverity.High,
                Description: "User-Agent indicates Windows but navigator.platform doesn't match",
                Field: "navigator.platform",
                ExpectedValue: "Win32 or similar",
                ActualValue: navigator.Platform,
                SuspiciousnessContribution: 20,
                Recommendation: "Ensure navigator.platform matches User-Agent OS information"
            ));
        }

        if (userAgent.Contains("Macintosh") && !navigator.Platform.Contains("Mac"))
        {
            navigatorConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.NavigatorInconsistency,
                Severity: AnomalySeverity.High,
                Description: "User-Agent indicates macOS but navigator.platform doesn't match",
                Field: "navigator.platform",
                ExpectedValue: "MacIntel or similar",
                ActualValue: navigator.Platform,
                SuspiciousnessContribution: 20,
                Recommendation: "Ensure navigator.platform matches User-Agent OS information"
            ));
        }

        // Validate vendor consistency
        var browser = FingerprintUtils.GetBrowserFromUserAgent(userAgent);
        var expectedVendor = browser switch
        {
            "chrome" => "Google Inc.",
            "firefox" => "",
            "safari" => "Apple Computer, Inc.",
            "edge" => "Microsoft Corporation",
            _ => null
        };

        if (expectedVendor != null && navigator.Vendor != expectedVendor)
        {
            navigatorConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.NavigatorInconsistency,
                Severity: AnomalySeverity.Medium,
                Description: $"Navigator vendor doesn't match expected value for {browser}",
                Field: "navigator.vendor",
                ExpectedValue: expectedVendor,
                ActualValue: navigator.Vendor,
                SuspiciousnessContribution: 10,
                Recommendation: $"Set navigator.vendor to '{expectedVendor}' for {browser}"
            ));
        }

        // Validate webdriver property
        if (navigator.Webdriver.ToLowerInvariant() == "true")
        {
            navigatorConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.NavigatorInconsistency,
                Severity: AnomalySeverity.Critical,
                Description: "Navigator webdriver property indicates automated browser",
                Field: "navigator.webdriver",
                ExpectedValue: "false or undefined",
                ActualValue: navigator.Webdriver,
                SuspiciousnessContribution: 50,
                Recommendation: "Set navigator.webdriver to false to avoid detection"
            ));
        }

        validations.Add(new ValidationCheck("Navigator Consistency", navigatorConsistencyPassed, 
            "Validates navigator properties match User-Agent", 
            "Navigator properties consistent with User-Agent", 
            $"Platform: {navigator.Platform}, Vendor: {navigator.Vendor}, WebDriver: {navigator.Webdriver}", 
            navigatorConsistencyPassed ? AnomalySeverity.Low : AnomalySeverity.High));
    }

    /// <summary>
    /// Validates font availability matches platform expectations
    /// </summary>
    private static void ValidateFontConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var fonts = fingerprint.Fonts;
        var platform = fingerprint.Navigator.Platform.ToLowerInvariant();
        var fontConsistencyPassed = true;

        // Platform-specific font validation
        if (platform.Contains("win"))
        {
            var expectedWindowsFonts = new[] { "Arial", "Times New Roman", "Calibri", "Segoe UI" };
            var missingFonts = expectedWindowsFonts.Where(f => !fonts.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList();
            
            if (missingFonts.Any())
            {
                fontConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.FontInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: $"Missing common Windows fonts: {string.Join(", ", missingFonts)}",
                    Field: "fonts",
                    ExpectedValue: string.Join(", ", expectedWindowsFonts),
                    ActualValue: $"Missing: {string.Join(", ", missingFonts)}",
                    SuspiciousnessContribution: 5,
                    Recommendation: "Include common Windows system fonts"
                ));
            }
        }

        if (platform.Contains("mac"))
        {
            var expectedMacFonts = new[] { "Helvetica", "Times", "Arial", "Helvetica Neue" };
            var missingFonts = expectedMacFonts.Where(f => !fonts.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList();
            
            if (missingFonts.Any())
            {
                fontConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.FontInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: $"Missing common macOS fonts: {string.Join(", ", missingFonts)}",
                    Field: "fonts",
                    ExpectedValue: string.Join(", ", expectedMacFonts),
                    ActualValue: $"Missing: {string.Join(", ", missingFonts)}",
                    SuspiciousnessContribution: 5,
                    Recommendation: "Include common macOS system fonts"
                ));
            }

            // Check for Windows-specific fonts on Mac
            var windowsOnlyFonts = new[] { "Segoe UI", "Calibri", "Cambria" };
            var suspiciousFonts = fonts.Where(f => windowsOnlyFonts.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList();
            
            if (suspiciousFonts.Any())
            {
                fontConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.FontInconsistency,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Windows-specific fonts found on macOS: {string.Join(", ", suspiciousFonts)}",
                    Field: "fonts",
                    ExpectedValue: "macOS-appropriate fonts only",
                    ActualValue: string.Join(", ", suspiciousFonts),
                    SuspiciousnessContribution: 12,
                    Recommendation: "Remove Windows-specific fonts from macOS fingerprint"
                ));
            }
        }

        // Check for unrealistic font count
        if (fonts.Count < 10)
        {
            fontConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.FontInconsistency,
                Severity: AnomalySeverity.Low,
                Description: $"Font count {fonts.Count} is unusually low",
                Field: "fonts",
                ExpectedValue: "15-100 fonts",
                ActualValue: $"{fonts.Count} fonts",
                SuspiciousnessContribution: 3,
                Recommendation: "Include more fonts to appear realistic"
            ));
        }
        else if (fonts.Count > 200)
        {
            fontConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.FontInconsistency,
                Severity: AnomalySeverity.Low,
                Description: $"Font count {fonts.Count} is unusually high",
                Field: "fonts",
                ExpectedValue: "15-100 fonts",
                ActualValue: $"{fonts.Count} fonts",
                SuspiciousnessContribution: 4,
                Recommendation: "Reduce font count to appear more realistic"
            ));
        }

        validations.Add(new ValidationCheck("Font Consistency", fontConsistencyPassed, 
            "Validates fonts are appropriate for the platform", 
            "Platform-appropriate fonts", $"{fonts.Count} fonts for {platform}", 
            fontConsistencyPassed ? AnomalySeverity.Low : AnomalySeverity.Low));
    }

    /// <summary>
    /// Validates battery information realism
    /// </summary>
    private static void ValidateBatteryRealism(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        if (fingerprint.Battery == null)
        {
            validations.Add(new ValidationCheck("Battery Information", true, "No battery information to validate", "N/A", "N/A", AnomalySeverity.Low));
            return;
        }

        var battery = fingerprint.Battery;
        var batteryRealisticPassed = true;

        // Validate battery level
        if (battery.TryGetValue("level", out var levelObj) && levelObj is double level)
        {
            if (level < 0 || level > 1)
            {
                batteryRealisticPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.UnrealisticBattery,
                    Severity: AnomalySeverity.Medium,
                    Description: $"Battery level {level} is outside valid range (0-1)",
                    Field: "battery.level",
                    ExpectedValue: "0.0-1.0",
                    ActualValue: level.ToString(CultureInfo.InvariantCulture),
                    SuspiciousnessContribution: 8,
                    Recommendation: "Set battery level between 0.0 and 1.0"
                ));
            }
        }

        // Validate charging/discharging time consistency
        if (battery.TryGetValue("charging", out var chargingObj) && chargingObj is bool charging)
        {
            var hasChargingTime = battery.TryGetValue("chargingTime", out var chargingTimeObj);
            var hasDischargingTime = battery.TryGetValue("dischargingTime", out var dischargingTimeObj);

            if (charging)
            {
                // When charging, chargingTime should be finite, dischargingTime should be Infinity
                if (hasDischargingTime && dischargingTimeObj is double dischargingTime && !double.IsPositiveInfinity(dischargingTime))
                {
                    batteryRealisticPassed = false;
                    anomalies.Add(new AnomalyReport(
                        Type: AnomalyType.UnrealisticBattery,
                        Severity: AnomalySeverity.Low,
                        Description: "Battery is charging but dischargingTime is not Infinity",
                        Field: "battery.dischargingTime",
                        ExpectedValue: "Infinity when charging",
                        ActualValue: dischargingTime.ToString(CultureInfo.InvariantCulture),
                        SuspiciousnessContribution: 5,
                        Recommendation: "Set dischargingTime to Infinity when battery is charging"
                    ));
                }
            }
            else
            {
                // When not charging, dischargingTime should be finite, chargingTime should be Infinity
                if (hasChargingTime && chargingTimeObj is double chargingTime && !double.IsPositiveInfinity(chargingTime))
                {
                    batteryRealisticPassed = false;
                    anomalies.Add(new AnomalyReport(
                        Type: AnomalyType.UnrealisticBattery,
                        Severity: AnomalySeverity.Low,
                        Description: "Battery is not charging but chargingTime is not Infinity",
                        Field: "battery.chargingTime",
                        ExpectedValue: "Infinity when not charging",
                        ActualValue: chargingTime.ToString(CultureInfo.InvariantCulture),
                        SuspiciousnessContribution: 5,
                        Recommendation: "Set chargingTime to Infinity when battery is not charging"
                    ));
                }
            }
        }

        validations.Add(new ValidationCheck("Battery Information", batteryRealisticPassed, 
            "Validates battery information is realistic", "Realistic battery values", 
            $"Level: {battery.GetValueOrDefault("level", "N/A")}, Charging: {battery.GetValueOrDefault("charging", "N/A")}", 
            batteryRealisticPassed ? AnomalySeverity.Low : AnomalySeverity.Medium));
    }

    /// <summary>
    /// Validates multimedia device consistency
    /// </summary>
    private static void ValidateMultimediaConsistency(Fingerprint fingerprint, List<ValidationCheck> validations, List<AnomalyReport> anomalies)
    {
        var devices = fingerprint.MultimediaDevices;
        var platform = fingerprint.Navigator.Platform.ToLowerInvariant();
        var multimediaConsistencyPassed = true;

        // Platform-specific device validation
        if (platform.Contains("win"))
        {
            var hasRealtekAudio = devices.Any(d => d.Contains("Realtek", StringComparison.OrdinalIgnoreCase));
            var hasDefaultDevices = devices.Any(d => d.Contains("Default", StringComparison.OrdinalIgnoreCase));

            if (!hasRealtekAudio && !hasDefaultDevices)
            {
                multimediaConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.MultimediaInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: "Windows typically has Realtek audio or Default devices",
                    Field: "multimediaDevices",
                    ExpectedValue: "Realtek Audio or Default devices",
                    ActualValue: string.Join(", ", devices.Take(3)),
                    SuspiciousnessContribution: 4,
                    Recommendation: "Include typical Windows audio devices"
                ));
            }
        }

        if (platform.Contains("mac"))
        {
            var hasBuiltIn = devices.Any(d => d.Contains("Built-in", StringComparison.OrdinalIgnoreCase));
            var hasMacBook = devices.Any(d => d.Contains("MacBook", StringComparison.OrdinalIgnoreCase));

            if (!hasBuiltIn && !hasMacBook)
            {
                multimediaConsistencyPassed = false;
                anomalies.Add(new AnomalyReport(
                    Type: AnomalyType.MultimediaInconsistency,
                    Severity: AnomalySeverity.Low,
                    Description: "macOS typically has Built-in or MacBook devices",
                    Field: "multimediaDevices",
                    ExpectedValue: "Built-in or MacBook devices",
                    ActualValue: string.Join(", ", devices.Take(3)),
                    SuspiciousnessContribution: 4,
                    Recommendation: "Include typical macOS audio devices"
                ));
            }
        }

        // Check for unrealistic device count
        if (devices.Count == 0)
        {
            multimediaConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.MultimediaInconsistency,
                Severity: AnomalySeverity.Medium,
                Description: "No multimedia devices detected",
                Field: "multimediaDevices",
                ExpectedValue: "At least 1-2 audio devices",
                ActualValue: "0 devices",
                SuspiciousnessContribution: 10,
                Recommendation: "Include at least basic audio input/output devices"
            ));
        }
        else if (devices.Count > 20)
        {
            multimediaConsistencyPassed = false;
            anomalies.Add(new AnomalyReport(
                Type: AnomalyType.MultimediaInconsistency,
                Severity: AnomalySeverity.Low,
                Description: $"Unusually high number of multimedia devices: {devices.Count}",
                Field: "multimediaDevices",
                ExpectedValue: "2-10 devices",
                ActualValue: $"{devices.Count} devices",
                SuspiciousnessContribution: 3,
                Recommendation: "Reduce number of multimedia devices to appear more realistic"
            ));
        }

        validations.Add(new ValidationCheck("Multimedia Devices", multimediaConsistencyPassed, 
            "Validates multimedia devices are appropriate for platform", 
            "Platform-appropriate multimedia devices", $"{devices.Count} devices", 
            multimediaConsistencyPassed ? AnomalySeverity.Low : AnomalySeverity.Low));
    }

    /// <summary>
    /// Calculates overall suspiciousness score based on detected anomalies
    /// </summary>
    private static int CalculateSuspiciousnessScore(List<AnomalyReport> anomalies)
    {
        if (!anomalies.Any())
            return 0;

        var totalScore = anomalies.Sum(a => a.SuspiciousnessContribution);
        
        // Apply severity multipliers
        var severityMultiplier = anomalies.Max(a => (int)a.Severity) switch
        {
            4 => 1.5, // Critical
            3 => 1.3, // High
            2 => 1.1, // Medium
            _ => 1.0   // Low
        };

        var finalScore = (int)(totalScore * severityMultiplier);
        return Math.Min(100, finalScore); // Cap at 100
    }
}