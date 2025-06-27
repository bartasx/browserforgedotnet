using BrowserforgeDotnet.BayesianNetwork;
using System.Text.Json;

namespace BrowserforgeDotnet.Headers;

/// <summary>
/// Generates HTTP headers based on a set of constraints using Bayesian Networks
/// </summary>
public class HeaderGenerator
{
    private readonly BrowserforgeDotnet.BayesianNetwork.BayesianNetwork _inputGeneratorNetwork;
    private readonly BrowserforgeDotnet.BayesianNetwork.BayesianNetwork _headerGeneratorNetwork;
    private readonly List<HttpBrowserObject> _uniqueBrowsers;
    private readonly Dictionary<string, List<string>> _headersOrder;

    /// <summary>
    /// Default configuration options for the header generator
    /// </summary>
    public HeaderGeneratorOptions Options { get; private set; }

    /// <summary>
    /// Initializes a new instance of the HeaderGenerator class
    /// </summary>
    /// <param name="inputNetworkPath">Path to the input generator network file</param>
    /// <param name="headerNetworkPath">Path to the header generator network file</param>
    /// <param name="browserHelperPath">Path to the browser helper file</param>
    /// <param name="headersOrderPath">Path to the headers order file</param>
    /// <param name="options">Optional configuration for the generator</param>
    public HeaderGenerator(
        string inputNetworkPath,
        string headerNetworkPath,
        string browserHelperPath,
        string headersOrderPath,
        HeaderGeneratorOptions? options = null)
    {
        _inputGeneratorNetwork = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(inputNetworkPath);
        _headerGeneratorNetwork = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(headerNetworkPath);
        
        Options = options ?? new HeaderGeneratorOptions();
        _uniqueBrowsers = LoadUniqueBrowsers(browserHelperPath);
        _headersOrder = LoadHeadersOrder(headersOrderPath);
    }

    /// <summary>
    /// Initializes a new instance of the HeaderGenerator class with network definitions
    /// </summary>
    /// <param name="inputNetworkDefinition">Input generator network definition</param>
    /// <param name="headerNetworkDefinition">Header generator network definition</param>
    /// <param name="uniqueBrowserStrings">List of unique browser strings</param>
    /// <param name="headersOrder">Dictionary of header orders by browser</param>
    /// <param name="options">Optional configuration for the generator</param>
    public HeaderGenerator(
        Dictionary<string, object> inputNetworkDefinition,
        Dictionary<string, object> headerNetworkDefinition,
        List<string> uniqueBrowserStrings,
        Dictionary<string, List<string>> headersOrder,
        HeaderGeneratorOptions? options = null)
    {
        _inputGeneratorNetwork = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(inputNetworkDefinition);
        _headerGeneratorNetwork = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(headerNetworkDefinition);
        
        Options = options ?? new HeaderGeneratorOptions();
        _uniqueBrowsers = uniqueBrowserStrings
            .Where(s => s != HeaderConstants.MissingValueDatasetToken)
            .Select(HttpBrowserObject.FromString)
            .ToList();
        _headersOrder = headersOrder;
    }

    /// <summary>
    /// Generates HTTP headers using the default options and optional overrides
    /// </summary>
    /// <param name="overrides">Optional overrides for generation parameters</param>
    /// <returns>Dictionary of generated HTTP headers</returns>
    public Dictionary<string, string> Generate(HeaderGenerationRequest? overrides = null)
    {
        var effectiveOptions = MergeOptions(Options, overrides);
        var generated = GetHeaders(effectiveOptions);
        
        if (effectiveOptions.HttpVersion == "2")
        {
            return HeaderUtils.PascalizeHeaders(generated);
        }
        
        return generated;
    }

    /// <summary>
    /// Internal method to generate headers based on effective options
    /// </summary>
    /// <param name="options">Effective options for header generation</param>
    /// <returns>Dictionary of generated headers</returns>
    private Dictionary<string, string> GetHeaders(EffectiveHeaderOptions options)
    {
        var possibleAttributeValues = GetPossibleAttributeValues(options);
        
        Dictionary<string, IEnumerable<string>>? http1Values = null;
        Dictionary<string, IEnumerable<string>>? http2Values = null;

        if (options.UserAgent?.Any() == true)
        {
            http1Values = _headerGeneratorNetwork.GetPossibleValues(
                new Dictionary<string, IEnumerable<string>> { { "User-Agent", options.UserAgent } });
            http2Values = _headerGeneratorNetwork.GetPossibleValues(
                new Dictionary<string, IEnumerable<string>> { { "user-agent", options.UserAgent } });
        }

        var constraints = PrepareConstraints(possibleAttributeValues, http1Values, http2Values);
        
        var inputSample = _inputGeneratorNetwork.GenerateConsistentSampleWhenPossible(constraints);
        
        if (inputSample == null)
        {
            if (options.HttpVersion == "1")
            {
                // Fallback to HTTP/2 and convert
                var fallbackOptions = options with { HttpVersion = "2" };
                var headers2 = GetHeaders(fallbackOptions);
                return OrderHeaders(HeaderUtils.PascalizeHeaders(headers2));
            }

            // Try relaxing constraints
            var relaxedOptions = RelaxConstraints(options);
            if (relaxedOptions != null)
            {
                return GetHeaders(relaxedOptions);
            }

            if (options.Strict)
            {
                throw new InvalidOperationException(
                    "No headers based on this input can be generated. Please relax or change some of the requirements you specified.");
            }

            // Return minimal headers as fallback
            return new Dictionary<string, string>
            {
                { options.HttpVersion == "2" ? "user-agent" : "User-Agent", "Mozilla/5.0" }
            };
        }

        var generatedSample = _headerGeneratorNetwork.GenerateSample(inputSample);
        var generatedHttpAndBrowser = HttpBrowserObject.FromString(
            generatedSample["*BROWSER_HTTP"]?.ToString() ?? string.Empty);

        // Add Accept-Language header
        var acceptLanguageFieldName = generatedHttpAndBrowser.IsHttp2 ? "accept-language" : "Accept-Language";
        generatedSample[acceptLanguageFieldName] = HeaderUtils.GenerateAcceptLanguageHeader(options.Locales);

        // Add Sec-Fetch headers if appropriate
        if (HeaderUtils.ShouldAddSecFetch(generatedHttpAndBrowser))
        {
            var secFetchHeaders = generatedHttpAndBrowser.IsHttp2 
                ? HeaderConstants.Http2SecFetchAttributes 
                : HeaderConstants.Http1SecFetchAttributes;
                
            foreach (var kvp in secFetchHeaders)
            {
                generatedSample[kvp.Key] = kvp.Value;
            }
        }

        // Filter out unwanted headers
        var filteredSample = generatedSample
            .Where(kvp => !ShouldFilterHeader(kvp.Key, kvp.Value?.ToString()))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);

        // Add request-dependent headers
        if (options.RequestDependentHeaders?.Any() == true)
        {
            foreach (var kvp in options.RequestDependentHeaders)
            {
                filteredSample[kvp.Key] = kvp.Value;
            }
        }

        return OrderHeaders(filteredSample);
    }

    /// <summary>
    /// Orders headers according to browser-specific ordering
    /// </summary>
    /// <param name="headers">Headers to order</param>
    /// <returns>Ordered headers dictionary</returns>
    private Dictionary<string, string> OrderHeaders(Dictionary<string, string> headers)
    {
        var userAgent = HeaderUtils.GetUserAgent(headers);
        if (userAgent == null)
        {
            throw new InvalidOperationException("Failed to find User-Agent in generated response");
        }

        var browserName = HeaderUtils.GetBrowser(userAgent);
        if (browserName == null)
        {
            throw new InvalidOperationException("Failed to find browser in User-Agent");
        }

        if (!_headersOrder.TryGetValue(browserName, out var headerOrder))
        {
            return headers;
        }

        var orderedHeaders = new Dictionary<string, string>();
        
        // Add headers in the specified order
        foreach (var headerName in headerOrder)
        {
            if (headers.TryGetValue(headerName, out var headerValue))
            {
                orderedHeaders[headerName] = headerValue;
            }
        }

        // Add any remaining headers not in the order list
        foreach (var kvp in headers)
        {
            if (!orderedHeaders.ContainsKey(kvp.Key))
            {
                orderedHeaders[kvp.Key] = kvp.Value;
            }
        }

        return orderedHeaders;
    }

    /// <summary>
    /// Gets possible attribute values based on the effective options
    /// </summary>
    /// <param name="options">Effective options</param>
    /// <returns>Dictionary of possible attribute values</returns>
    private Dictionary<string, IEnumerable<string>> GetPossibleAttributeValues(EffectiveHeaderOptions options)
    {
        var browserHttpOptions = GetBrowserHttpOptions(options.Browsers);
        
        var result = new Dictionary<string, IEnumerable<string>>
        {
            ["*BROWSER_HTTP"] = browserHttpOptions,
            ["*OPERATING_SYSTEM"] = options.OperatingSystems
        };

        if (options.Devices.Any())
        {
            result["*DEVICE"] = options.Devices;
        }

        return result;
    }

    /// <summary>
    /// Gets browser HTTP options based on browser specifications
    /// </summary>
    /// <param name="browsers">Browser specifications</param>
    /// <returns>List of browser HTTP option strings</returns>
    private List<string> GetBrowserHttpOptions(IEnumerable<Browser> browsers)
    {
        var result = new List<string>();
        
        foreach (var browser in browsers)
        {
            var matchingOptions = _uniqueBrowsers
                .Where(browserOption =>
                    browser.Name == browserOption.Name &&
                    (!browser.MinVersion.HasValue || browser.MinVersion <= browserOption.MajorVersion) &&
                    (!browser.MaxVersion.HasValue || browser.MaxVersion >= browserOption.MajorVersion) &&
                    (string.IsNullOrEmpty(browser.HttpVersion) || browser.HttpVersion == browserOption.HttpVersion))
                .Select(browserOption => browserOption.CompleteString);
                
            result.AddRange(matchingOptions);
        }
        
        return result;
    }

    /// <summary>
    /// Prepares constraints for consistent sample generation
    /// </summary>
    /// <param name="possibleAttributeValues">Possible attribute values</param>
    /// <param name="http1Values">HTTP/1 constraints</param>
    /// <param name="http2Values">HTTP/2 constraints</param>
    /// <returns>Dictionary of constraints</returns>
    private Dictionary<string, IEnumerable<string>> PrepareConstraints(
        Dictionary<string, IEnumerable<string>> possibleAttributeValues,
        Dictionary<string, IEnumerable<string>>? http1Values,
        Dictionary<string, IEnumerable<string>>? http2Values)
    {
        var constraints = new Dictionary<string, IEnumerable<string>>();
        
        foreach (var kvp in possibleAttributeValues)
        {
            var key = kvp.Key;
            var values = kvp.Value;
            
            IEnumerable<string> filteredValues;
            
            if (key == "*BROWSER_HTTP")
            {
                filteredValues = values.Where(value => 
                    HeaderUtils.FilterBrowserHttp(value, http1Values, http2Values));
            }
            else
            {
                filteredValues = values.Where(value => 
                    HeaderUtils.FilterOtherValues(value, http1Values, http2Values, key));
            }
            
            constraints[key] = filteredValues.ToList();
        }
        
        return constraints;
    }

    /// <summary>
    /// Attempts to relax constraints when generation fails
    /// </summary>
    /// <param name="options">Current options</param>
    /// <returns>Relaxed options or null if no more relaxation possible</returns>
    private EffectiveHeaderOptions? RelaxConstraints(EffectiveHeaderOptions options)
    {
        // Try relaxing constraints in the predefined order
        if (options.Locales.Count() > 1)
        {
            return options with { Locales = new[] { "en-US" } };
        }
        
        if (options.Devices.Count() > 1)
        {
            return options with { Devices = HeaderConstants.SupportedDevices };
        }
        
        if (options.OperatingSystems.Count() > 1)
        {
            return options with { OperatingSystems = HeaderConstants.SupportedOperatingSystems };
        }
        
        if (options.Browsers.Count() > 1)
        {
            return options with { Browsers = HeaderConstants.SupportedBrowsers.Select(b => Browser.FromName(b)) };
        }
        
        return null;
    }

    /// <summary>
    /// Determines if a header should be filtered out
    /// </summary>
    /// <param name="key">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>True if the header should be filtered out</returns>
    private static bool ShouldFilterHeader(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;
            
        if (key.StartsWith("*"))
            return true;
            
        if (value == HeaderConstants.MissingValueDatasetToken)
            return true;
            
        if (key.Equals("connection", StringComparison.OrdinalIgnoreCase) && 
            value.Equals("close", StringComparison.OrdinalIgnoreCase))
            return true;
            
        return false;
    }

    /// <summary>
    /// Merges default options with request overrides
    /// </summary>
    /// <param name="defaultOptions">Default options</param>
    /// <param name="overrides">Request overrides</param>
    /// <returns>Effective options for header generation</returns>
    private static EffectiveHeaderOptions MergeOptions(HeaderGeneratorOptions defaultOptions, HeaderGenerationRequest? overrides)
    {
        var browsers = overrides?.Browsers?.Any() == true
            ? PrepareBrowsersConfig(overrides.Browsers, overrides.HttpVersion ?? defaultOptions.HttpVersion)
            : PrepareBrowsersConfig(defaultOptions.Browsers, defaultOptions.HttpVersion);

        return new EffectiveHeaderOptions(
            Browsers: browsers,
            OperatingSystems: overrides?.OperatingSystems ?? defaultOptions.OperatingSystems,
            Devices: overrides?.Devices ?? defaultOptions.Devices,
            Locales: overrides?.Locales ?? defaultOptions.Locales,
            HttpVersion: overrides?.HttpVersion ?? defaultOptions.HttpVersion,
            Strict: overrides?.Strict ?? defaultOptions.Strict,
            UserAgent: overrides?.UserAgent,
            RequestDependentHeaders: overrides?.RequestDependentHeaders
        );
    }

    /// <summary>
    /// Prepares browser configuration from various input types
    /// </summary>
    /// <param name="browsers">Browser specifications</param>
    /// <param name="httpVersion">HTTP version</param>
    /// <returns>List of Browser objects</returns>
    private static List<Browser> PrepareBrowsersConfig(IEnumerable<object> browsers, string httpVersion)
    {
        return browsers.Select(browser => browser switch
        {
            Browser b => b,
            string s => Browser.FromName(s, httpVersion),
            _ => Browser.FromName(browser.ToString() ?? "chrome", httpVersion)
        }).ToList();
    }

    /// <summary>
    /// Loads unique browsers from the browser helper file
    /// </summary>
    /// <param name="browserHelperPath">Path to browser helper file</param>
    /// <returns>List of HttpBrowserObject instances</returns>
    private static List<HttpBrowserObject> LoadUniqueBrowsers(string browserHelperPath)
    {
        var json = File.ReadAllText(browserHelperPath);
        var browserStrings = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        
        return browserStrings
            .Where(s => s != HeaderConstants.MissingValueDatasetToken)
            .Select(HttpBrowserObject.FromString)
            .ToList();
    }

    /// <summary>
    /// Loads headers order from the headers order file
    /// </summary>
    /// <param name="headersOrderPath">Path to headers order file</param>
    /// <returns>Dictionary of header orders by browser</returns>
    private static Dictionary<string, List<string>> LoadHeadersOrder(string headersOrderPath)
    {
        var json = File.ReadAllText(headersOrderPath);
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? 
               new Dictionary<string, List<string>>();
    }
}