using System.Text.Json;
using BrowserforgeDotnet.BayesianNetwork;
using BrowserforgeDotnet.Headers;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Generates realistic browser fingerprints using Bayesian Networks and header generation
/// </summary>
public class FingerprintGenerator
{
    private readonly BrowserforgeDotnet.BayesianNetwork.BayesianNetwork _fingerprintGeneratorNetwork;
    private readonly HeaderGenerator _headerGenerator;

    // Default configuration
    private readonly Screen? _defaultScreen;
    private readonly bool _defaultStrict;
    private readonly bool _defaultMockWebRTC;
    private readonly bool _defaultSlim;

    /// <summary>
    /// Initializes a new instance of the FingerprintGenerator
    /// </summary>
    /// <param name="fingerprintNetwork">Bayesian network for fingerprint generation</param>
    /// <param name="headerGenerator">Header generator instance</param>
    /// <param name="screen">Default screen constraints</param>
    /// <param name="strict">Default strict mode setting</param>
    /// <param name="mockWebRTC">Default WebRTC mocking setting</param>
    /// <param name="slim">Default slim mode setting</param>
    public FingerprintGenerator(
        BrowserforgeDotnet.BayesianNetwork.BayesianNetwork fingerprintNetwork,
        HeaderGenerator headerGenerator,
        Screen? screen = null,
        bool strict = false,
        bool mockWebRTC = false,
        bool slim = false)
    {
        _fingerprintGeneratorNetwork = fingerprintNetwork ?? throw new ArgumentNullException(nameof(fingerprintNetwork));
        _headerGenerator = headerGenerator ?? throw new ArgumentNullException(nameof(headerGenerator));
        
        _defaultScreen = screen;
        _defaultStrict = strict;
        _defaultMockWebRTC = mockWebRTC;
        _defaultSlim = slim;
    }

    /// <summary>
    /// Initializes a new instance of the FingerprintGenerator with file paths
    /// </summary>
    /// <param name="fingerprintNetworkPath">Path to the fingerprint network file</param>
    /// <param name="inputNetworkPath">Path to the input network file</param>
    /// <param name="headerNetworkPath">Path to the header network file</param>
    /// <param name="browserHelperPath">Path to the browser helper file</param>
    /// <param name="headersOrderPath">Path to the headers order file</param>
    /// <param name="options">Header generator options</param>
    /// <param name="screen">Default screen constraints</param>
    /// <param name="strict">Default strict mode setting</param>
    /// <param name="mockWebRTC">Default WebRTC mocking setting</param>
    /// <param name="slim">Default slim mode setting</param>
    public FingerprintGenerator(
        string fingerprintNetworkPath,
        string inputNetworkPath,
        string headerNetworkPath,
        string browserHelperPath,
        string headersOrderPath,
        HeaderGeneratorOptions? options = null,
        Screen? screen = null,
        bool strict = false,
        bool mockWebRTC = false,
        bool slim = false)
    {
        _fingerprintGeneratorNetwork = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(fingerprintNetworkPath);
        _headerGenerator = new HeaderGenerator(inputNetworkPath, headerNetworkPath, browserHelperPath, headersOrderPath, options);
        
        _defaultScreen = screen;
        _defaultStrict = strict;
        _defaultMockWebRTC = mockWebRTC;
        _defaultSlim = slim;
    }

    /// <summary>
    /// Generates a complete browser fingerprint
    /// </summary>
    /// <param name="screen">Screen constraints (overrides default)</param>
    /// <param name="strict">Strict mode setting (overrides default)</param>
    /// <param name="mockWebRTC">WebRTC mocking setting (overrides default)</param>
    /// <param name="slim">Slim mode setting (overrides default)</param>
    /// <param name="headerRequest">Header generation request parameters</param>
    /// <returns>Complete browser fingerprint</returns>
    public Fingerprint Generate(
        Screen? screen = null,
        bool? strict = null,
        bool? mockWebRTC = null,
        bool? slim = null,
        HeaderGenerationRequest? headerRequest = null)
    {
        // Resolve effective options
        var effectiveScreen = screen ?? _defaultScreen;
        var effectiveStrict = strict ?? _defaultStrict;
        var effectiveMockWebRTC = mockWebRTC ?? _defaultMockWebRTC;
        var effectiveSlim = slim ?? _defaultSlim;

        // Prepare filtered values for constraint satisfaction
        var filteredValues = new Dictionary<string, IEnumerable<string>>();
        
        // Get partial constraint satisfaction policy if screen constraints are set
        Dictionary<string, object>? partialCsp = null;
        if (effectiveScreen?.IsSet() == true)
        {
            partialCsp = GetPartialConstraintSatisfactionPolicy(effectiveStrict, effectiveScreen, filteredValues);
        }

        // Generate headers consistent with constraints
        if (partialCsp?.TryGetValue("userAgent", out var userAgentObj) == true && userAgentObj is string userAgent)
        {
            headerRequest = headerRequest with { UserAgent = new[] { userAgent } }
                ?? new HeaderGenerationRequest { UserAgent = new[] { userAgent } };
        }

        var headers = _headerGenerator.Generate(headerRequest);
        var extractedUserAgent = HeaderUtils.GetUserAgent(headers);
        
        if (string.IsNullOrEmpty(extractedUserAgent))
        {
            throw new InvalidOperationException("Failed to extract User-Agent from generated headers");
        }

        // Generate fingerprint consistent with the user agent
        Dictionary<string, object>? fingerprint = null;
        var attempts = 0;
        const int maxAttempts = 10;

        while (fingerprint == null && attempts < maxAttempts)
        {
            var constraints = new Dictionary<string, IEnumerable<string>>(filteredValues)
            {
                ["userAgent"] = new[] { extractedUserAgent }
            };

            fingerprint = _fingerprintGeneratorNetwork.GenerateConsistentSampleWhenPossible(constraints);
            
            if (fingerprint == null)
            {
                if (effectiveStrict)
                {
                    throw new InvalidOperationException(
                        "Cannot generate fingerprint. User-Agent may be invalid, or screen constraints are too restrictive.");
                }
                
                // Relax constraints progressively
                filteredValues.Clear();
                attempts++;
            }
        }

        if (fingerprint == null)
        {
            throw new InvalidOperationException("Failed to generate consistent fingerprint after multiple attempts");
        }

        // Process and clean up fingerprint data
        fingerprint = ProcessFingerprintData(fingerprint);

        // Add accept-language header based on generated languages
        var acceptLanguageHeader = FingerprintUtils.GenerateAcceptLanguageHeader(
            GetLanguagesFromFingerprint(fingerprint));
        
        var acceptLanguageKey = headers.ContainsKey("user-agent") ? "accept-language" : "Accept-Language";
        headers[acceptLanguageKey] = acceptLanguageHeader;

        // Add Sec-Fetch headers if appropriate
        if (FingerprintUtils.ShouldAddSecFetchHeaders(extractedUserAgent))
        {
            var httpVersion = headers.ContainsKey("user-agent") ? "2" : "1";
            var secFetchHeaders = FingerprintUtils.GetSecFetchHeaders(httpVersion);
            
            foreach (var kvp in secFetchHeaders)
            {
                headers[kvp.Key] = kvp.Value;
            }
        }

        return TransformFingerprint(fingerprint, headers, effectiveMockWebRTC, effectiveSlim);
    }

    /// <summary>
    /// Generates partial constraint satisfaction policy based on screen constraints
    /// </summary>
    /// <param name="strict">Whether to use strict constraint satisfaction</param>
    /// <param name="screen">Screen constraints</param>
    /// <param name="filteredValues">Dictionary to store filtered values</param>
    /// <returns>Partial CSP values or null if constraints cannot be satisfied</returns>
    private Dictionary<string, object>? GetPartialConstraintSatisfactionPolicy(
        bool strict, 
        Screen screen, 
        Dictionary<string, IEnumerable<string>> filteredValues)
    {
        if (!screen.IsSet())
            return null;

        // Get all possible screen values from the network
        if (!_fingerprintGeneratorNetwork.NodesByName.TryGetValue("screen", out var screenNode))
        {
            return null;
        }

        var validScreenValues = screenNode.PossibleValues
            .Where(screenString => FingerprintUtils.IsScreenWithinConstraints(screenString, screen))
            .ToList();

        if (!validScreenValues.Any())
        {
            if (strict)
            {
                throw new InvalidOperationException("Screen constraints are too restrictive");
            }
            return null;
        }

        filteredValues["screen"] = validScreenValues;

        try
        {
            // Use the network's constraint propagation to get possible values
            var possibleValues = _fingerprintGeneratorNetwork.GetPossibleValues(filteredValues);
            
            // Convert to the expected format for partial CSP
            var result = new Dictionary<string, object>();
            
            foreach (var kvp in possibleValues)
            {
                var valuesList = kvp.Value.ToList();
                if (valuesList.Count == 1)
                {
                    result[kvp.Key] = valuesList[0];
                }
                else if (valuesList.Any())
                {
                    // For multiple values, we could return the first one or implement more sophisticated selection
                    result[kvp.Key] = valuesList[0];
                }
            }
            
            return result.Any() ? result : null;
        }
        catch (Exception)
        {
            if (strict)
                throw;
            
            // Remove screen constraint and try again
            filteredValues.Remove("screen");
            return null;
        }
    }

    /// <summary>
    /// Processes fingerprint data by cleaning up missing values and unpacking stringified data
    /// </summary>
    /// <param name="fingerprint">Raw fingerprint data</param>
    /// <returns>Processed fingerprint data</returns>
    private static Dictionary<string, object> ProcessFingerprintData(Dictionary<string, object> fingerprint)
    {
        var processed = new Dictionary<string, object>();

        foreach (var kvp in fingerprint)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            // Handle missing values
            if (value?.ToString() == FingerprintConstants.MissingValueDatasetToken)
            {
                processed[key] = null!;
                continue;
            }

            // Handle stringified JSON values
            if (value is string stringValue && stringValue.StartsWith(FingerprintConstants.StringifiedPrefix))
            {
                try
                {
                    var jsonString = stringValue.Substring(FingerprintConstants.StringifiedPrefix.Length);
                    var parsedValue = JsonSerializer.Deserialize<object>(jsonString);
                    processed[key] = parsedValue ?? null!;
                }
                catch (JsonException)
                {
                    processed[key] = value;
                }
            }
            else
            {
                processed[key] = value;
            }
        }

        return processed;
    }

    /// <summary>
    /// Extracts languages from fingerprint data for Accept-Language header generation
    /// </summary>
    /// <param name="fingerprint">Fingerprint data</param>
    /// <returns>List of language codes</returns>
    private static List<string> GetLanguagesFromFingerprint(Dictionary<string, object> fingerprint)
    {
        if (fingerprint.TryGetValue("languages", out var languagesObj))
        {
            return languagesObj switch
            {
                List<string> stringList => stringList,
                List<object> objectList => objectList.Select(o => o?.ToString() ?? "en-US").ToList(),
                string[] stringArray => stringArray.ToList(),
                object[] objectArray => objectArray.Select(o => o?.ToString() ?? "en-US").ToList(),
                string single => new List<string> { single },
                _ => new List<string> { "en-US", "en" }
            };
        }

        // Fallback to language field or default
        if (fingerprint.TryGetValue("language", out var languageObj) && languageObj is string language)
        {
            return new List<string> { language, language.Split('-')[0] };
        }

        return new List<string> { "en-US", "en" };
    }

    /// <summary>
    /// Transforms the raw fingerprint data into a structured Fingerprint object
    /// </summary>
    /// <param name="fingerprintData">Raw fingerprint data</param>
    /// <param name="headers">Generated headers</param>
    /// <param name="mockWebRTC">WebRTC mocking setting</param>
    /// <param name="slim">Slim mode setting</param>
    /// <returns>Structured Fingerprint object</returns>
    private static Fingerprint TransformFingerprint(
        Dictionary<string, object> fingerprintData,
        Dictionary<string, string> headers,
        bool mockWebRTC,
        bool slim)
    {
        // Extract languages and set the primary language
        var languages = GetLanguagesFromFingerprint(fingerprintData);
        fingerprintData["languages"] = languages;
        fingerprintData["language"] = languages.FirstOrDefault() ?? "en-US";

        // Generate additional fingerprint components if not present
        if (!fingerprintData.ContainsKey("battery") || fingerprintData["battery"] == null)
        {
            fingerprintData["battery"] = FingerprintUtils.GenerateBatteryInfo();
        }

        if (!fingerprintData.ContainsKey("multimediaDevices"))
        {
            var platform = fingerprintData.TryGetValue("platform", out var platformObj) 
                ? platformObj?.ToString() ?? "Win32" 
                : "Win32";
            fingerprintData["multimediaDevices"] = FingerprintUtils.GenerateMultimediaDevices(platform);
        }

        if (!fingerprintData.ContainsKey("fonts"))
        {
            var platform = fingerprintData.TryGetValue("platform", out var platformObj) 
                ? platformObj?.ToString() ?? "Win32" 
                : "Win32";
            fingerprintData["fonts"] = FingerprintUtils.FilterFontsForPlatform(FingerprintConstants.CommonFonts, platform);
        }

        return Fingerprint.FromDictionary(fingerprintData, headers, mockWebRTC, slim);
    }

    /// <summary>
    /// Creates a FingerprintGenerator with default configuration for testing
    /// </summary>
    /// <returns>FingerprintGenerator instance with minimal configuration</returns>
    public static FingerprintGenerator CreateDefault()
    {
        // Create a minimal network for testing
        var networkDefinition = new Dictionary<string, object>
        {
            ["nodes"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "userAgent",
                    ["type"] = "categorical",
                    ["possible_values"] = new List<string>
                    {
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
                    },
                    ["probabilities"] = new List<double> { 1.0 }
                }
            }
        };

        var network = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(networkDefinition);
        
        // Create minimal header generator networks
        var inputNetworkDefinition = new Dictionary<string, object>
        {
            ["nodes"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "*BROWSER_HTTP",
                    ["type"] = "categorical",
                    ["possible_values"] = new List<string> { "chrome|108|2" },
                    ["probabilities"] = new List<double> { 1.0 }
                }
            }
        };

        var headerNetworkDefinition = new Dictionary<string, object>
        {
            ["nodes"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "User-Agent",
                    ["type"] = "categorical",
                    ["possible_values"] = new List<string>
                    {
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
                    },
                    ["probabilities"] = new List<double> { 1.0 }
                }
            }
        };

        var uniqueBrowsers = new List<string> { "chrome/108.0.0.0|2" };
        var headersOrder = new Dictionary<string, List<string>>
        {
            ["chrome"] = new List<string> { "User-Agent", "Accept", "Accept-Language" }
        };

        var headerGenerator = new HeaderGenerator(
            inputNetworkDefinition,
            headerNetworkDefinition,
            uniqueBrowsers,
            headersOrder);

        return new FingerprintGenerator(network, headerGenerator);
    }
}