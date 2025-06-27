using Xunit;
using FluentAssertions;
using BrowserforgeDotnet.Fingerprints;

namespace BrowserforgeDotnet.Tests;

/// <summary>
/// Unit tests for fingerprint validation and anomaly detection functionality
/// </summary>
public class FingerprintValidationTests
{
    /// <summary>
    /// Tests that a valid fingerprint passes all validation checks
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithValidFingerprint_ShouldPassValidation()
    {
        // Arrange
        var validFingerprint = CreateValidFingerprint();

        // Act
        var result = FingerprintValidator.ValidateFingerprint(validFingerprint);

        // Assert
        result.IsValid.Should().BeTrue();
        result.SuspiciousnessScore.Should().BeLessThan(30);
        result.Anomalies.Should().HaveCountLessThan(3);
    }

    /// <summary>
    /// Tests browser-platform consistency validation
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithSafariOnWindows_ShouldDetectInconsistency()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var safariUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15";
        var invalidNavigator = fingerprint.Navigator with 
        { 
            UserAgent = safariUserAgent, 
            Platform = "Win32" // Safari on Windows - suspicious!
        };
        var invalidFingerprint = fingerprint with { Navigator = invalidNavigator };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.BrowserPlatformInconsistency);
        result.SuspiciousnessScore.Should().BeGreaterThan(20);
    }

    /// <summary>
    /// Tests hardware realism validation with extreme values
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithUnrealisticHardware_ShouldDetectAnomalies()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var invalidNavigator = fingerprint.Navigator with 
        { 
            DeviceMemory = 128, // 128GB is unrealistic for typical devices
            HardwareConcurrency = 1 // 1 core with 128GB is impossible
        };
        var invalidFingerprint = fingerprint with { Navigator = invalidNavigator };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.UnrealisticHardware || a.Type == AnomalyType.ImpossibleHardware);
        result.SuspiciousnessScore.Should().BeGreaterThan(40);
    }

    /// <summary>
    /// Tests screen consistency validation
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithInconsistentScreen_ShouldDetectAnomalies()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var invalidScreen = fingerprint.Screen with 
        { 
            Width = 50000, // Unrealistic width
            Height = 50,   // Unrealistic height
            DevicePixelRatio = 10.0f // Impossible pixel ratio
        };
        var invalidFingerprint = fingerprint with { Screen = invalidScreen };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.ScreenInconsistency || a.Type == AnomalyType.ImpossibleHardware);
        result.SuspiciousnessScore.Should().BeGreaterThan(30);
    }

    /// <summary>
    /// Tests language consistency validation
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithInconsistentLanguages_ShouldDetectAnomalies()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var invalidNavigator = fingerprint.Navigator with 
        { 
            Language = "en-US",
            Languages = new List<string> { "fr-FR", "de-DE" } // Language doesn't match first in languages
        };
        var invalidHeaders = new Dictionary<string, string>(fingerprint.Headers)
        {
            ["Accept-Language"] = "es-ES,es;q=0.9" // Accept-Language doesn't match navigator.language
        };
        var invalidFingerprint = fingerprint with 
        { 
            Navigator = invalidNavigator,
            Headers = invalidHeaders
        };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.LanguageInconsistency);
        result.SuspiciousnessScore.Should().BeGreaterThan(10);
    }

    /// <summary>
    /// Tests navigator consistency validation with webdriver detection
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithWebdriverTrue_ShouldDetectCriticalAnomaly()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var invalidNavigator = fingerprint.Navigator with { Webdriver = "true" };
        var invalidFingerprint = fingerprint with { Navigator = invalidNavigator };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.NavigatorInconsistency && a.Severity == AnomalySeverity.Critical);
        result.SuspiciousnessScore.Should().BeGreaterThan(50);
    }

    /// <summary>
    /// Tests font consistency validation for platform
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithWindowsFontsOnMac_ShouldDetectInconsistency()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var macNavigator = fingerprint.Navigator with { Platform = "MacIntel" };
        var windowsFonts = new List<string> { "Segoe UI", "Calibri", "Arial" }; // Windows-specific fonts on Mac
        var invalidFingerprint = fingerprint with 
        { 
            Navigator = macNavigator,
            Fonts = windowsFonts
        };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.FontInconsistency);
    }

    /// <summary>
    /// Tests battery realism validation
    /// </summary>
    [Fact]
    public void ValidateFingerprint_WithUnrealisticBattery_ShouldDetectAnomalies()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var invalidBattery = new Dictionary<string, object>
        {
            { "level", 1.5 }, // Level > 1 is impossible
            { "charging", true },
            { "chargingTime", 50000 }, // Very long charging time
            { "dischargingTime", 1000 } // Should be infinity when charging
        };
        var invalidFingerprint = fingerprint with { Battery = invalidBattery };

        // Act
        var result = FingerprintValidator.ValidateFingerprint(invalidFingerprint);

        // Assert
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.UnrealisticBattery);
    }

    /// <summary>
    /// Tests anomaly detection for statistical outliers
    /// </summary>
    [Fact]
    public void DetectAnomalies_WithStatisticalOutliers_ShouldIdentifyOutliers()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var outlierNavigator = fingerprint.Navigator with 
        { 
            DeviceMemory = 2, // Very low for modern systems
            HardwareConcurrency = 32 // Very high
        };
        var outlierFingerprint = fingerprint with 
        { 
            Navigator = outlierNavigator,
            Fonts = Enumerable.Range(1, 300).Select(i => $"Font{i}").ToList() // Too many fonts
        };

        // Act
        var anomalies = FingerprintAnomalyDetector.DetectAnomalies(outlierFingerprint);

        // Assert
        anomalies.Should().Contain(a => a.Type == AnomalyType.StatisticalOutlier);
    }

    /// <summary>
    /// Tests detection of automated generation patterns
    /// </summary>
    [Fact]
    public void DetectAnomalies_WithAutomatedPatterns_ShouldDetectAutomation()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var sortedFonts = new List<string> { "Arial", "Calibri", "Georgia", "Times New Roman" }; // Alphabetically sorted
        var templateLanguages = new List<string> { "en-US", "en" }; // Default template
        var automatedNavigator = fingerprint.Navigator with { Languages = templateLanguages };
        var automatedFingerprint = fingerprint with 
        { 
            Navigator = automatedNavigator,
            Fonts = sortedFonts
        };

        // Act
        var anomalies = FingerprintAnomalyDetector.DetectAnomalies(automatedFingerprint);

        // Assert
        anomalies.Should().Contain(a => a.Type == AnomalyType.AutomatedGeneration);
    }

    /// <summary>
    /// Tests detection of too common patterns
    /// </summary>
    [Fact]
    public void DetectAnomalies_WithTooCommonPatterns_ShouldDetectCommonness()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var commonScreen = fingerprint.Screen with 
        { 
            Width = 1920, 
            Height = 1080, 
            DevicePixelRatio = 1.0f 
        };
        var commonNavigator = fingerprint.Navigator with { DeviceMemory = 8 }; // Very common
        var commonFingerprint = fingerprint with 
        { 
            Screen = commonScreen,
            Navigator = commonNavigator
        };

        // Act
        var anomalies = FingerprintAnomalyDetector.DetectAnomalies(commonFingerprint);

        // Assert
        anomalies.Should().Contain(a => a.Type == AnomalyType.TooCommon);
    }

    /// <summary>
    /// Tests hardware consistency validation
    /// </summary>
    [Fact]
    public void ValidateHardwareConsistency_WithInconsistentHardware_ShouldDetectIssues()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var inconsistentNavigator = fingerprint.Navigator with 
        { 
            DeviceMemory = 2, // Low memory
            HardwareConcurrency = 16 // High CPU count - inconsistent
        };
        var inconsistentFingerprint = fingerprint with { Navigator = inconsistentNavigator };

        // Act
        var result = FingerprintUtils.ValidateHardwareConsistency(inconsistentFingerprint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Anomalies.Should().Contain(a => a.Type == AnomalyType.ImpossibleHardware);
    }

    /// <summary>
    /// Tests browser fingerprint realism assessment
    /// </summary>
    [Fact]
    public void CheckBrowserFingerprintRealism_WithRealisticFingerprint_ShouldScoreWell()
    {
        // Arrange
        var realisticFingerprint = CreateValidFingerprint();

        // Act
        var result = FingerprintUtils.CheckBrowserFingerprintRealism(realisticFingerprint);

        // Assert
        result.IsValid.Should().BeTrue();
        result.SuspiciousnessScore.Should().BeLessThan(40);
        result.Details.Should().ContainKey("realismAssessment");
    }

    /// <summary>
    /// Tests suspiciousness score calculation
    /// </summary>
    [Fact]
    public void GetSuspiciousnessScore_WithProblematicFingerprint_ShouldReturnHighScore()
    {
        // Arrange
        var fingerprint = CreateValidFingerprint();
        var problematicNavigator = fingerprint.Navigator with 
        { 
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
            Platform = "Win32", // Safari on Windows
            Webdriver = "true", // Webdriver detected
            DeviceMemory = 128 // Unrealistic memory
        };
        var problematicFingerprint = fingerprint with { Navigator = problematicNavigator };

        // Act
        var score = FingerprintUtils.GetSuspiciousnessScore(problematicFingerprint);

        // Assert
        score.OverallScore.Should().BeGreaterThan(60);
        score.ComponentScores.Should().ContainKey("browserPlatformConsistency");
        score.ComponentScores.Should().ContainKey("navigatorConsistency");
        score.RiskFactors.Should().NotBeEmpty();
        score.Recommendations.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests validation result helper methods
    /// </summary>
    [Fact]
    public void ValidationResult_HelperMethods_ShouldWorkCorrectly()
    {
        // Arrange
        var anomalies = new List<AnomalyReport>
        {
            new(AnomalyType.BrowserPlatformInconsistency, AnomalySeverity.Critical, "Critical issue", "field1", "expected", "actual", 50, "Fix it"),
            new(AnomalyType.ScreenInconsistency, AnomalySeverity.High, "High issue", "field2", "expected", "actual", 25, "Fix it"),
            new(AnomalyType.FontInconsistency, AnomalySeverity.Low, "Low issue", "field3", "expected", "actual", 5, "Fix it")
        };
        var validations = new List<ValidationCheck>();
        var result = ValidationResult.Invalid(anomalies, 80, validations);

        // Act & Assert
        result.GetMostCriticalAnomaly()?.Severity.Should().Be(AnomalySeverity.Critical);
        result.GetAnomaliesByType(AnomalyType.BrowserPlatformInconsistency).Should().HaveCount(1);
        result.GetRiskAssessment().Should().Contain("Critical Risk");
    }

    /// <summary>
    /// Creates a valid fingerprint for testing purposes
    /// </summary>
    private static Fingerprint CreateValidFingerprint()
    {
        var screen = ScreenFingerprint.Create(1920, 1080, 1.0f);
        var navigator = NavigatorFingerprint.CreateChrome(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36",
            "Win32",
            new List<string> { "en-US", "en" }
        );
        
        var headers = new Dictionary<string, string>
        {
            { "User-Agent", navigator.UserAgent },
            { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" },
            { "Accept-Language", "en-US,en;q=0.9" },
            { "Accept-Encoding", "gzip, deflate, br" }
        };

        var videoCodecs = new Dictionary<string, string>
        {
            { "video/mp4", "probably" },
            { "video/webm", "probably" }
        };

        var audioCodecs = new Dictionary<string, string>
        {
            { "audio/mpeg", "probably" },
            { "audio/wav", "probably" },
            { "audio/ogg", "maybe" }
        };

        var fonts = new List<string>
        {
            "Arial", "Times New Roman", "Helvetica", "Georgia", "Verdana",
            "Calibri", "Segoe UI", "Tahoma", "Trebuchet MS", "Impact"
        };

        var multimediaDevices = new List<string>
        {
            "Default - Microphone (Realtek Audio)",
            "Default - Speakers (Realtek Audio)"
        };

        var battery = new Dictionary<string, object>
        {
            { "charging", true },
            { "chargingTime", 3600 },
            { "dischargingTime", double.PositiveInfinity },
            { "level", 0.85 }
        };

        return new Fingerprint(
            Screen: screen,
            Navigator: navigator,
            Headers: headers,
            VideoCodecs: videoCodecs,
            AudioCodecs: audioCodecs,
            PluginsData: new Dictionary<string, string>(),
            Battery: battery,
            VideoCard: VideoCard.CreateDefault(),
            MultimediaDevices: multimediaDevices,
            Fonts: fonts,
            MockWebRTC: false,
            Slim: false
        );
    }
}