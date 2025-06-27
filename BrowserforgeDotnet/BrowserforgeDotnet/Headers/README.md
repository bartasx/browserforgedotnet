# Headers Module

The Headers module provides HTTP header generation functionality for the BrowserForge .NET library. It generates realistic browser headers based on browser specifications, operating systems, devices, and other constraints.

## Key Components

### Core Classes

- **`HeaderGenerator`** - Main engine for generating HTTP headers using Bayesian Networks
- **`Browser`** - Represents browser specifications with version constraints
- **`HttpBrowserObject`** - Parsed browser information from header generation
- **`HeaderUtils`** - Utility functions for header processing and manipulation

### Configuration Types

- **`HeaderGeneratorOptions`** - Default configuration options
- **`HeaderGenerationRequest`** - Request-specific overrides
- **`HeaderGenerationRequestBuilder`** - Builder pattern for creating requests

### Constants

- **`HeaderConstants`** - Contains supported browsers, OS, devices, and other constants

## Key Features

### Supported Browsers
- Chrome
- Firefox
- Safari
- Edge

### Supported Operating Systems
- Windows
- macOS
- Linux
- Android
- iOS

### Supported Devices
- Desktop
- Mobile

### HTTP Protocol Support
- HTTP/1.1
- HTTP/2

## Usage Examples

### Basic Header Generation

```csharp
using BrowserforgeDotnet.Headers;

// Create generator with default options
var options = new HeaderGeneratorOptions
{
    Browsers = new[] { "chrome", "firefox" },
    OperatingSystems = new[] { "windows", "macos" },
    HttpVersion = "2"
};

var generator = new HeaderGenerator(
    inputNetworkPath: "path/to/input-network.json",
    headerNetworkPath: "path/to/header-network.json", 
    browserHelperPath: "path/to/browser-helper.json",
    headersOrderPath: "path/to/headers-order.json",
    options: options
);

// Generate headers
var headers = generator.Generate();
```

### Request-Specific Generation

```csharp
// Generate headers for a specific browser and OS
var request = new HeaderGenerationRequestBuilder()
    .WithBrowser("chrome")
    .WithOperatingSystem("windows")
    .WithDevice("desktop")
    .WithHttpVersion("2")
    .WithLocale("en-US")
    .Build();

var headers = generator.Generate(request);
```

### Browser Constraints

```csharp
// Generate headers for Chrome versions 100-120
var chromeBrowser = new Browser("chrome", minVersion: 100, maxVersion: 120, httpVersion: "2");

var request = new HeaderGenerationRequest
{
    Browsers = new[] { chromeBrowser }
};

var headers = generator.Generate(request);
```

## Advanced Features

### Constraint Relaxation
The generator automatically relaxes constraints when exact matches cannot be found, following this order:
1. Locales
2. Devices  
3. Operating Systems
4. Browsers

### Header Ordering
Headers are automatically ordered according to browser-specific conventions for maximum realism.

### Sec-Fetch Headers
Automatically adds appropriate Sec-Fetch headers based on browser version and capabilities:
- Chrome 76+
- Firefox 90+
- Edge 79+

### Accept-Language Generation
Generates realistic Accept-Language headers with quality values based on provided locales.

## Integration with Bayesian Networks

The Headers module integrates with the BayesianNetwork engine to:
- Generate statistically realistic header combinations
- Maintain consistency between browser, OS, and device characteristics
- Apply probabilistic constraints based on real-world browser usage patterns

## Error Handling

The generator includes robust error handling:
- **Strict Mode**: Throws exceptions when constraints cannot be satisfied
- **Relaxed Mode**: Automatically relaxes constraints to find valid combinations
- **Fallback Generation**: Provides minimal valid headers when all else fails

## Thread Safety

All classes in the Headers module are designed to be thread-safe for concurrent header generation.