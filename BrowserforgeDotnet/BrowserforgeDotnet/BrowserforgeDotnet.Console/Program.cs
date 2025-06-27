using BrowserforgeDotnet.Fingerprints;
using BrowserforgeDotnet.Headers;

namespace BrowserforgeDotnet.Console;

/// <summary>
/// Entry point for the BrowserForge .NET console application
/// Demonstrates usage of the Fingerprints module
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine("🌐 BrowserForge .NET - Fingerprints Module Demo");
        System.Console.WriteLine("================================================");
        System.Console.WriteLine();

        try
        {
            // Demonstrate basic fingerprint creation
            DemonstrateBasicFingerprint();
            
            System.Console.WriteLine();
            
            // Demonstrate screen constraints
            DemonstrateScreenConstraints();
            
            System.Console.WriteLine();
            
            // Demonstrate navigator fingerprints
            DemonstrateNavigatorFingerprints();
            
            System.Console.WriteLine();
            
            // Demonstrate utility functions
            DemonstrateUtilityFunctions();
            
            System.Console.WriteLine();
            
            // Demonstrate fingerprint validation
            DemonstrateFingerprintValidation();
            
            System.Console.WriteLine();
            
            // Demonstrate anomaly detection
            DemonstrateAnomalyDetection();
            
            System.Console.WriteLine();
            
            // Demonstrate suspicious fingerprint analysis
            DemonstrateSuspiciousFingerprintAnalysis();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Demo completed! Press any key to exit...");
        System.Console.ReadKey();
    }

    private static void DemonstrateBasicFingerprint()
    {
        System.Console.WriteLine("📱 Basic Fingerprint Creation");
        System.Console.WriteLine("-----------------------------");

        // Create a basic fingerprint
        var fingerprint = Fingerprint.CreateBasic();
        
        System.Console.WriteLine($"Screen Resolution: {fingerprint.Screen.Width}x{fingerprint.Screen.Height}");
        System.Console.WriteLine($"User Agent: {fingerprint.Navigator.UserAgent}");
        System.Console.WriteLine($"Language: {fingerprint.Navigator.Language}");
        System.Console.WriteLine($"Platform: {fingerprint.Navigator.Platform}");
        System.Console.WriteLine($"Hardware Concurrency: {fingerprint.Navigator.HardwareConcurrency}");
        System.Console.WriteLine($"Device Memory: {fingerprint.Navigator.DeviceMemory?.ToString() ?? "Not available"}");
        System.Console.WriteLine($"Video Codecs: {fingerprint.VideoCodecs.Count} available");
        System.Console.WriteLine($"Audio Codecs: {fingerprint.AudioCodecs.Count} available");
        
        // Show JSON serialization
        System.Console.WriteLine();
        System.Console.WriteLine("📄 JSON Output (first 200 chars):");
        var json = fingerprint.ToJson();
        var preview = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
        System.Console.WriteLine(preview);
    }

    private static void DemonstrateScreenConstraints()
    {
        System.Console.WriteLine("🖥️  Screen Constraints Demo");
        System.Console.WriteLine("---------------------------");

        // Create different screen constraints
        var desktopScreen = Screen.Desktop();
        var mobileScreen = Screen.Mobile();
        var customScreen = new Screen(minWidth: 1440, maxWidth: 2560, minHeight: 900, maxHeight: 1440);

        System.Console.WriteLine($"Desktop constraints: Min {desktopScreen.MinWidth}x{desktopScreen.MinHeight}");
        System.Console.WriteLine($"Mobile constraints: Max {mobileScreen.MaxWidth}x{mobileScreen.MaxHeight}");
        System.Console.WriteLine($"Custom constraints: {customScreen.MinWidth}-{customScreen.MaxWidth} x {customScreen.MinHeight}-{customScreen.MaxHeight}");

        // Test constraint satisfaction
        var testWidth = 1920;
        var testHeight = 1080;
        System.Console.WriteLine($"Testing {testWidth}x{testHeight}:");
        System.Console.WriteLine($"  Desktop: {desktopScreen.SatisfiesConstraints(testWidth, testHeight)}");
        System.Console.WriteLine($"  Mobile: {mobileScreen.SatisfiesConstraints(testWidth, testHeight)}");
        System.Console.WriteLine($"  Custom: {customScreen.SatisfiesConstraints(testWidth, testHeight)}");
    }

    private static void DemonstrateNavigatorFingerprints()
    {
        System.Console.WriteLine("🧭 Navigator Fingerprints Demo");
        System.Console.WriteLine("------------------------------");

        var chromeUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        var firefoxUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0";

        // Create Chrome navigator
        var chromeNav = NavigatorFingerprint.CreateChrome(chromeUA);
        System.Console.WriteLine($"Chrome Navigator:");
        System.Console.WriteLine($"  Vendor: {chromeNav.Vendor}");
        System.Console.WriteLine($"  Product: {chromeNav.Product}");
        System.Console.WriteLine($"  Device Memory: {chromeNav.DeviceMemory}");
        System.Console.WriteLine($"  User Agent Data: {(chromeNav.UserAgentData.ContainsKey("brands") ? "Available" : "Not available")}");

        // Create Firefox navigator  
        var firefoxNav = NavigatorFingerprint.CreateFirefox(firefoxUA);
        System.Console.WriteLine($"Firefox Navigator:");
        System.Console.WriteLine($"  Vendor: {firefoxNav.Vendor}");
        System.Console.WriteLine($"  Product Sub: {firefoxNav.ProductSub}");
        System.Console.WriteLine($"  Do Not Track: {firefoxNav.DoNotTrack}");
        System.Console.WriteLine($"  Device Memory: {firefoxNav.DeviceMemory?.ToString() ?? "Not exposed"}");
    }

    private static void DemonstrateUtilityFunctions()
    {
        System.Console.WriteLine("🔧 Utility Functions Demo");
        System.Console.WriteLine("-------------------------");

        // Browser detection
        var userAgents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15"
        };

        System.Console.WriteLine("Browser Detection:");
        foreach (var ua in userAgents)
        {
            var browser = FingerprintUtils.GetBrowserFromUserAgent(ua);
            var shortUA = ua.Length > 50 ? ua.Substring(0, 50) + "..." : ua;
            System.Console.WriteLine($"  {shortUA} -> {browser}");
        }

        // Accept-Language generation
        System.Console.WriteLine();
        System.Console.WriteLine("Accept-Language Generation:");
        var locales = new[] { "en-US", "en", "fr", "de" };
        var acceptLang = FingerprintUtils.GenerateAcceptLanguageHeader(locales);
        System.Console.WriteLine($"  Input: [{string.Join(", ", locales)}]");
        System.Console.WriteLine($"  Output: {acceptLang}");

        // Battery simulation
        System.Console.WriteLine();
        System.Console.WriteLine("Battery Simulation:");
        var battery = FingerprintUtils.GenerateBatteryInfo();
        System.Console.WriteLine($"  Charging: {battery["charging"]}");
        System.Console.WriteLine($"  Level: {battery["level"]}");
        System.Console.WriteLine($"  Charging Time: {battery["chargingTime"]}");

        // Font filtering
        System.Console.WriteLine();
        System.Console.WriteLine("Font Filtering by Platform:");
        var availableFonts = new List<string> { "Arial", "Helvetica", "Times New Roman", "Ubuntu", "Segoe UI" };
        var winFonts = FingerprintUtils.FilterFontsForPlatform(availableFonts, "Win32");
        System.Console.WriteLine($"  Windows fonts: {string.Join(", ", winFonts.Take(3))}...");

        // Multimedia devices
        System.Console.WriteLine();
        System.Console.WriteLine("Multimedia Devices:");
        var devices = FingerprintUtils.GenerateMultimediaDevices("Win32");
        System.Console.WriteLine($"  Device count: {devices.Count}");
        System.Console.WriteLine($"  First device: {devices.FirstOrDefault()}");

        // Constants demonstration
        System.Console.WriteLine();
        System.Console.WriteLine("Available Constants:");
        System.Console.WriteLine($"  Video codecs: {FingerprintConstants.VideoCodecs.Count}");
        System.Console.WriteLine($"  Audio codecs: {FingerprintConstants.AudioCodecs.Count}");
        System.Console.WriteLine($"  Common plugins: {FingerprintConstants.CommonPlugins.Count}");
        System.Console.WriteLine($"  Common fonts: {FingerprintConstants.CommonFonts.Count}");
        System.Console.WriteLine($"  Common resolutions: {FingerprintConstants.CommonResolutions.Count}");
    }

    private static void DemonstrateFingerprintValidation()
    {
        System.Console.WriteLine("🔍 Fingerprint Validation Demo");
        System.Console.WriteLine("------------------------------");

        // Create a valid fingerprint
        var validFingerprint = Fingerprint.CreateBasic();
        System.Console.WriteLine("✅ Validating a good fingerprint:");
        
        var validResult = FingerprintValidator.ValidateFingerprint(validFingerprint);
        System.Console.WriteLine($"  Is Valid: {validResult.IsValid}");
        System.Console.WriteLine($"  Suspiciousness Score: {validResult.SuspiciousnessScore}/100");
        System.Console.WriteLine($"  Anomalies Found: {validResult.Anomalies.Count}");
        System.Console.WriteLine($"  Risk Assessment: {validResult.GetRiskAssessment()}");

        System.Console.WriteLine();

        // Create a suspicious fingerprint (Safari on Windows)
        System.Console.WriteLine("❌ Validating a suspicious fingerprint (Safari on Windows):");
        var suspiciousNavigator = validFingerprint.Navigator with
        {
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
            Platform = "Win32",
            Vendor = "Apple Computer, Inc.",
            Webdriver = "true" // Also add webdriver detection
        };
        var suspiciousFingerprint = validFingerprint with { Navigator = suspiciousNavigator };
        
        var suspiciousResult = FingerprintValidator.ValidateFingerprint(suspiciousFingerprint);
        System.Console.WriteLine($"  Is Valid: {suspiciousResult.IsValid}");
        System.Console.WriteLine($"  Suspiciousness Score: {suspiciousResult.SuspiciousnessScore}/100");
        System.Console.WriteLine($"  Anomalies Found: {suspiciousResult.Anomalies.Count}");
        System.Console.WriteLine($"  Risk Assessment: {suspiciousResult.GetRiskAssessment()}");
        
        if (suspiciousResult.Anomalies.Any())
        {
            System.Console.WriteLine("  Top Issues:");
            foreach (var anomaly in suspiciousResult.Anomalies.Take(3))
            {
                System.Console.WriteLine($"    • {anomaly.Type}: {anomaly.Description}");
            }
        }
    }

    private static void DemonstrateAnomalyDetection()
    {
        System.Console.WriteLine("🚨 Anomaly Detection Demo");
        System.Console.WriteLine("-------------------------");

        var baseFingerprint = Fingerprint.CreateBasic();

        // Test 1: Statistical Outliers
        System.Console.WriteLine("🔢 Testing Statistical Outliers:");
        var outlierNavigator = baseFingerprint.Navigator with
        {
            DeviceMemory = 128, // 128GB - statistical outlier
            HardwareConcurrency = 1 // 1 core with 128GB - impossible combination
        };
        var outlierFingerprint = baseFingerprint with { Navigator = outlierNavigator };
        
        var outlierAnomalies = FingerprintAnomalyDetector.DetectAnomalies(outlierFingerprint);
        var statisticalAnomalies = outlierAnomalies.Where(a => a.Type == AnomalyType.StatisticalOutlier).ToList();
        System.Console.WriteLine($"  Statistical outliers found: {statisticalAnomalies.Count}");
        foreach (var anomaly in statisticalAnomalies.Take(2))
        {
            System.Console.WriteLine($"    • {anomaly.Description}");
        }

        System.Console.WriteLine();

        // Test 2: Automated Generation Patterns
        System.Console.WriteLine("🤖 Testing Automated Generation Patterns:");
        var sortedFonts = new List<string> { "Arial", "Calibri", "Georgia", "Helvetica", "Times New Roman" };
        var templateLanguages = new List<string> { "en-US", "en" };
        var automatedNavigator = baseFingerprint.Navigator with { Languages = templateLanguages };
        var automatedFingerprint = baseFingerprint with
        {
            Navigator = automatedNavigator,
            Fonts = sortedFonts
        };
        
        var automatedAnomalies = FingerprintAnomalyDetector.DetectAnomalies(automatedFingerprint);
        var automationAnomalies = automatedAnomalies.Where(a => a.Type == AnomalyType.AutomatedGeneration).ToList();
        System.Console.WriteLine($"  Automation patterns found: {automationAnomalies.Count}");
        foreach (var anomaly in automationAnomalies.Take(2))
        {
            System.Console.WriteLine($"    • {anomaly.Description}");
        }

        System.Console.WriteLine();

        // Test 3: Too Common Patterns
        System.Console.WriteLine("📊 Testing Too Common Patterns:");
        var commonScreen = baseFingerprint.Screen with
        {
            Width = 1920,
            Height = 1080,
            DevicePixelRatio = 1.0f
        };
        var commonNavigator = baseFingerprint.Navigator with { DeviceMemory = 8 };
        var commonFingerprint = baseFingerprint with
        {
            Screen = commonScreen,
            Navigator = commonNavigator
        };
        
        var commonAnomalies = FingerprintAnomalyDetector.DetectAnomalies(commonFingerprint);
        var tooCommonAnomalies = commonAnomalies.Where(a => a.Type == AnomalyType.TooCommon).ToList();
        System.Console.WriteLine($"  Common patterns found: {tooCommonAnomalies.Count}");
        foreach (var anomaly in tooCommonAnomalies.Take(2))
        {
            System.Console.WriteLine($"    • {anomaly.Description}");
        }
    }

    private static void DemonstrateSuspiciousFingerprintAnalysis()
    {
        System.Console.WriteLine("🕵️ Suspicious Fingerprint Analysis Demo");
        System.Console.WriteLine("---------------------------------------");

        // Create a highly suspicious fingerprint
        var baseFingerprint = Fingerprint.CreateBasic();
        var suspiciousNavigator = baseFingerprint.Navigator with
        {
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
            Platform = "Win32", // Safari on Windows
            DeviceMemory = 256, // Unrealistic memory
            HardwareConcurrency = 1, // Impossible with high memory
            Webdriver = "true", // Automation detected
            Languages = new List<string> { "en-US", "en" } // Template languages
        };

        var suspiciousScreen = baseFingerprint.Screen with
        {
            Width = 1920,
            Height = 1080,
            DevicePixelRatio = 1.0f // Too perfect
        };

        var windowsFonts = new List<string> { "Segoe UI", "Calibri", "Arial" }; // Windows fonts on "Mac"
        
        var suspiciousFingerprint = baseFingerprint with
        {
            Navigator = suspiciousNavigator,
            Screen = suspiciousScreen,
            Fonts = windowsFonts
        };

        // Get comprehensive analysis
        System.Console.WriteLine("🔍 Comprehensive Analysis:");
        var realismResult = FingerprintUtils.CheckBrowserFingerprintRealism(suspiciousFingerprint);
        System.Console.WriteLine($"  Overall Realistic: {realismResult.IsValid}");
        System.Console.WriteLine($"  Suspiciousness Score: {realismResult.SuspiciousnessScore}/100");

        if (realismResult.Details.TryGetValue("realismAssessment", out var assessmentObj))
        {
            System.Console.WriteLine($"  Assessment: {assessmentObj}");
        }

        System.Console.WriteLine();

        // Get suspiciousness score breakdown
        System.Console.WriteLine("📊 Suspiciousness Score Breakdown:");
        var scoreBreakdown = FingerprintUtils.GetSuspiciousnessScore(suspiciousFingerprint);
        System.Console.WriteLine($"  Overall Score: {scoreBreakdown.OverallScore}/100");
        
        System.Console.WriteLine("  Component Scores:");
        foreach (var component in scoreBreakdown.ComponentScores.Where(c => c.Value > 0))
        {
            System.Console.WriteLine($"    • {component.Key}: {component.Value}");
        }

        System.Console.WriteLine();

        // Show risk factors
        System.Console.WriteLine("⚠️  Risk Factors:");
        foreach (var risk in scoreBreakdown.RiskFactors.Take(5))
        {
            System.Console.WriteLine($"  • {risk}");
        }

        System.Console.WriteLine();

        // Show recommendations
        System.Console.WriteLine("💡 Recommendations:");
        foreach (var recommendation in scoreBreakdown.Recommendations.Take(5))
        {
            System.Console.WriteLine($"  • {recommendation}");
        }

        System.Console.WriteLine();

        // Hardware consistency check
        System.Console.WriteLine("🔧 Hardware Consistency Analysis:");
        var hardwareResult = FingerprintUtils.ValidateHardwareConsistency(suspiciousFingerprint);
        System.Console.WriteLine($"  Hardware Consistent: {hardwareResult.IsValid}");
        System.Console.WriteLine($"  Hardware Issues: {hardwareResult.Anomalies.Count}");
        
        foreach (var issue in hardwareResult.Anomalies.Take(3))
        {
            System.Console.WriteLine($"    • {issue.Description}");
        }

        System.Console.WriteLine();

        // Show detection likelihood
        var detectionRisk = realismResult.SuspiciousnessScore switch
        {
            >= 80 => "🔴 CRITICAL - Almost certainly detected",
            >= 60 => "🟠 HIGH - Likely to be detected",
            >= 40 => "🟡 MEDIUM - May be detected",
            >= 20 => "🟢 LOW - Unlikely to be detected",
            _ => "🟢 MINIMAL - Very unlikely to be detected"
        };

        System.Console.WriteLine($"🎯 Detection Risk: {detectionRisk}");
    }
}