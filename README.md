# BrowserForge .NET

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/nuget/v/BrowserforgeDotnet.svg)](https://www.nuget.org/packages/BrowserforgeDotnet)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/github/workflow/status/browserforge/browserforge-dotnet/CI)](https://github.com/browserforge/browserforge-dotnet/actions)

A comprehensive .NET library for generating realistic browser fingerprints and HTTP headers using advanced Bayesian networks. This is a feature-complete port of the popular Python BrowserForge library, designed to help developers create authentic browser automation scenarios while avoiding detection.

## 🌟 Features

- **🧠 Bayesian Network Engine**: Advanced statistical modeling for generating correlated browser characteristics
- **🔍 Fingerprint Generation**: Create realistic browser fingerprints for Chrome, Firefox, Safari, and Edge
- **📊 Anomaly Detection**: Built-in validation and suspicious pattern detection
- **🌐 HTTP Header Generation**: Generate authentic browser headers with proper correlations
- **🎭 Browser Automation Integration**: Seamless integration with Playwright, Pyppeteer, and other frameworks
- **📱 Multi-Platform Support**: Comprehensive Windows, macOS, and Linux browser characteristics
- **🔧 Extensive Validation**: Hardware consistency checks and realism assessment
- **⚡ High Performance**: Optimized for .NET 9.0 with minimal memory footprint

## 📦 Installation

### NuGet Package Manager
```powershell
Install-Package BrowserforgeDotnet
```

### .NET CLI
```bash
dotnet add package BrowserforgeDotnet
```

### PackageReference
```xml
<PackageReference Include="BrowserforgeDotnet" Version="1.0.0" />
```

## 🚀 Quick Start

### Basic Fingerprint Generation

```csharp
using BrowserforgeDotnet.Fingerprints;

// Generate a basic realistic fingerprint
var fingerprint = Fingerprint.CreateBasic();

Console.WriteLine($"User Agent: {fingerprint.Navigator.UserAgent}");
Console.WriteLine($"Screen: {fingerprint.Screen.Width}x{fingerprint.Screen.Height}");
Console.WriteLine($"Platform: {fingerprint.Navigator.Platform}");
Console.WriteLine($"Languages: {string.Join(", ", fingerprint.Navigator.Languages)}");

// Export to JSON
var json = fingerprint.ToJson();
```

### HTTP Headers Generation

```csharp
using BrowserforgeDotnet.Headers;

// Generate realistic HTTP headers
var headers = HeaderGenerator.GenerateHeaders(BrowserType.Chrome);

foreach (var header in headers)
{
    Console.WriteLine($"{header.Key}: {header.Value}");
}
```

### Browser-Specific Fingerprints

```csharp
// Generate Chrome fingerprint
var chromeFingerprint = Fingerprint.CreateChrome();

// Generate Firefox fingerprint  
var firefoxFingerprint = Fingerprint.CreateFirefox();

// Generate Safari fingerprint
var safariFingerprint = Fingerprint.CreateSafari();
```

## 🏗️ Architecture Overview

BrowserForge .NET uses a sophisticated Bayesian network approach to ensure statistical consistency across all generated browser characteristics. The library analyzes real-world browser data patterns to create fingerprints that are indistinguishable from genuine browsers.

### Core Components

- **Bayesian Network Engine**: Probabilistic modeling for correlated data generation
- **Fingerprint Validation**: Multi-layered validation system for realism assessment
- **Anomaly Detection**: Pattern recognition for identifying suspicious characteristics
- **Browser Injectors**: Ready-to-use integrations for popular automation frameworks

## 📚 Detailed Usage Examples

### Screen Constraints

```csharp
using BrowserforgeDotnet.Fingerprints;

// Desktop screen constraints
var desktopScreen = Screen.Desktop();
var fingerprint = Fingerprint.CreateWithScreen(desktopScreen);

// Mobile screen constraints
var mobileScreen = Screen.Mobile();
var mobileFingerprint = Fingerprint.CreateWithScreen(mobileScreen);

// Custom screen constraints
var customScreen = new Screen(
    minWidth: 1440, 
    maxWidth: 2560, 
    minHeight: 900, 
    maxHeight: 1440
);
```

### Advanced Fingerprint Validation

```csharp
using BrowserforgeDotnet.Fingerprints;

var fingerprint = Fingerprint.CreateBasic();

// Comprehensive validation
var validationResult = FingerprintValidator.ValidateFingerprint(fingerprint);

Console.WriteLine($"Is Valid: {validationResult.IsValid}");
Console.WriteLine($"Suspiciousness Score: {validationResult.SuspiciousnessScore}/100");
Console.WriteLine($"Risk Assessment: {validationResult.GetRiskAssessment()}");

// Check for anomalies
if (validationResult.Anomalies.Any())
{
    Console.WriteLine("Detected anomalies:");
    foreach (var anomaly in validationResult.Anomalies)
    {
        Console.WriteLine($"  - {anomaly.Type}: {anomaly.Description}");
    }
}
```

### Hardware Consistency Validation

```csharp
// Validate hardware consistency
var hardwareResult = FingerprintUtils.ValidateHardwareConsistency(fingerprint);

Console.WriteLine($"Hardware Consistent: {hardwareResult.IsValid}");
foreach (var issue in hardwareResult.Anomalies)
{
    Console.WriteLine($"Issue: {issue.Description}");
}
```

### Realism Assessment

```csharp
// Comprehensive realism check
var realismResult = FingerprintUtils.CheckBrowserFingerprintRealism(fingerprint);

Console.WriteLine($"Realistic: {realismResult.IsValid}");
Console.WriteLine($"Suspiciousness Score: {realismResult.SuspiciousnessScore}/100");

// Get detailed breakdown
var scoreBreakdown = FingerprintUtils.GetSuspiciousnessScore(fingerprint);
Console.WriteLine($"Overall Score: {scoreBreakdown.OverallScore}/100");

Console.WriteLine("Risk Factors:");
foreach (var risk in scoreBreakdown.RiskFactors)
{
    Console.WriteLine($"  - {risk}");
}

Console.WriteLine("Recommendations:");
foreach (var recommendation in scoreBreakdown.Recommendations)
{
    Console.WriteLine($"  - {recommendation}");
}
```

## 🔧 API Reference

### Core Classes

#### [`Fingerprint`](BrowserforgeDotnet/Fingerprints/Fingerprint.cs)
The main class for browser fingerprint generation and manipulation.

```csharp
public class Fingerprint
{
    public NavigatorFingerprint Navigator { get; set; }
    public ScreenFingerprint Screen { get; set; }
    public List<string> Fonts { get; set; }
    public List<string> VideoCodecs { get; set; }
    public List<string> AudioCodecs { get; set; }
    
    // Factory methods
    public static Fingerprint CreateBasic()
    public static Fingerprint CreateChrome()
    public static Fingerprint CreateFirefox()
    public static Fingerprint CreateSafari()
    public static Fingerprint CreateEdge()
    
    // Export methods
    public string ToJson()
    public Dictionary<string, object> ToDictionary()
}
```

#### [`NavigatorFingerprint`](BrowserforgeDotnet/Fingerprints/NavigatorFingerprint.cs)
Represents browser navigator properties.

```csharp
public record NavigatorFingerprint
{
    public string UserAgent { get; init; }
    public string Platform { get; init; }
    public string Language { get; init; }
    public List<string> Languages { get; init; }
    public int HardwareConcurrency { get; init; }
    public int? DeviceMemory { get; init; }
    public string Vendor { get; init; }
    public Dictionary<string, object> UserAgentData { get; init; }
}
```

#### [`FingerprintValidator`](BrowserforgeDotnet/Fingerprints/FingerprintValidator.cs)
Provides comprehensive fingerprint validation capabilities.

```csharp
public static class FingerprintValidator
{
    public static ValidationResult ValidateFingerprint(Fingerprint fingerprint)
    public static List<Anomaly> DetectAnomalies(Fingerprint fingerprint)
    public static SuspiciousnessScore GetSuspiciousnessScore(Fingerprint fingerprint)
}
```

### Utility Functions

#### [`FingerprintUtils`](BrowserforgeDotnet/Utils/FingerprintUtils.cs)
Utility functions for fingerprint manipulation and analysis.

```csharp
public static class FingerprintUtils
{
    public static BrowserType GetBrowserFromUserAgent(string userAgent)
    public static string GenerateAcceptLanguageHeader(List<string> languages)
    public static Dictionary<string, object> GenerateBatteryInfo()
    public static List<string> FilterFontsForPlatform(List<string> fonts, string platform)
    public static ValidationResult CheckBrowserFingerprintRealism(Fingerprint fingerprint)
    public static ValidationResult ValidateHardwareConsistency(Fingerprint fingerprint)
}
```

## 🔗 Integration Examples

### Playwright Integration

```csharp
using Microsoft.Playwright;
using BrowserforgeDotnet.Injectors.Playwright;

// Create realistic fingerprint
var fingerprint = Fingerprint.CreateChrome();

// Launch browser with fingerprint
var browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false
});

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();

// Inject fingerprint
await PlaywrightInjector.InjectFingerprint(page, fingerprint);

// Your automation code here
await page.GotoAsync("https://example.com");
```

### Pyppeteer Integration

```csharp
using BrowserforgeDotnet.Injectors.Pyppeteer;

// Generate fingerprint and inject into Pyppeteer
var fingerprint = Fingerprint.CreateChrome();
await PyppeteerInjector.InjectFingerprint(page, fingerprint);
```

### Custom HTTP Client

```csharp
using BrowserforgeDotnet.Headers;

var headers = HeaderGenerator.GenerateHeaders(BrowserType.Chrome);
var client = new HttpClient();

foreach (var header in headers)
{
    client.DefaultRequestHeaders.Add(header.Key, header.Value);
}
```

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Fingerprints"
```

### Test Categories

- **BayesianNetworkTests**: Core Bayesian network functionality
- **FingerprintsTests**: Fingerprint generation and validation
- **HeadersTests**: HTTP header generation
- **FingerprintValidationTests**: Validation and anomaly detection
- **IntegrationTests**: Browser automation integration tests

### Writing Custom Tests

```csharp
[Fact]
public void Should_Generate_Valid_Chrome_Fingerprint()
{
    // Arrange & Act
    var fingerprint = Fingerprint.CreateChrome();
    
    // Assert
    fingerprint.Should().NotBeNull();
    fingerprint.Navigator.UserAgent.Should().Contain("Chrome");
    
    var validation = FingerprintValidator.ValidateFingerprint(fingerprint);
    validation.IsValid.Should().BeTrue();
    validation.SuspiciousnessScore.Should().BeLessThan(30);
}
```

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

1. **Fork the repository** and create a feature branch
2. **Write tests** for any new functionality
3. **Follow C# coding standards** and use consistent formatting
4. **Update documentation** for any API changes
5. **Submit a pull request** with a clear description

### Development Setup

```bash
# Clone the repository
git clone https://github.com/browserforge/browserforge-dotnet.git
cd browserforge-dotnet

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Code Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new features

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Credits and Acknowledgments

This .NET library is a port of the original Python [BrowserForge](https://github.com/daijro/browserforge) project. Special thanks to:

- **[daijro](https://github.com/daijro)** - Original Python BrowserForge author
- **BrowserForge Contributors** - The original Python library contributors
- **Microsoft** - For the excellent .NET ecosystem
- **xUnit, FluentAssertions, Moq** - Testing framework contributors

## 🔧 System Requirements

- **.NET 9.0** or later
- **Memory**: Minimum 512MB RAM
- **Storage**: ~50MB for the library and dependencies
- **Supported OS**: Windows, macOS, Linux

## 📊 Performance Characteristics

- **Fingerprint Generation**: ~1-5ms per fingerprint
- **Validation**: ~10-50ms per fingerprint
- **Memory Usage**: ~10-50MB typical usage
- **Thread Safety**: All public APIs are thread-safe

## 🆘 Support

- **Documentation**: [Wiki](https://github.com/browserforge/browserforge-dotnet/wiki)
- **Issues**: [GitHub Issues](https://github.com/browserforge/browserforge-dotnet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/browserforge/browserforge-dotnet/discussions)

## 🗺️ Roadmap

- [ ] Additional browser support (Opera, Brave)
- [ ] Enhanced mobile browser fingerprints
- [ ] WebRTC fingerprint generation
- [ ] Canvas fingerprint generation
- [ ] Audio fingerprint generation
- [ ] Machine learning-based anomaly detection improvements

---
