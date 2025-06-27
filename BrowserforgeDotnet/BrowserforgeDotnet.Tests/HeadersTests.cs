using BrowserforgeDotnet.Headers;
using Xunit;
using FluentAssertions;

namespace BrowserforgeDotnet.Tests;

/// <summary>
/// Tests for the Headers module functionality
/// </summary>
public class HeadersTests
{
    [Fact]
    public void Browser_Constructor_ShouldCreateValidBrowser()
    {
        // Arrange & Act
        var browser = new Browser("chrome", minVersion: 80, maxVersion: 120, httpVersion: "2");

        // Assert
        browser.Name.Should().Be("chrome");
        browser.MinVersion.Should().Be(80);
        browser.MaxVersion.Should().Be(120);
        browser.HttpVersion.Should().Be("2");
    }

    [Fact]
    public void Browser_Constructor_ShouldThrowWhenMinVersionExceedsMaxVersion()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new Browser("chrome", minVersion: 120, maxVersion: 80));
        
        exception.Message.Should().Contain("min version constraint");
        exception.Message.Should().Contain("cannot exceed max version");
    }

    [Fact]
    public void Browser_FromName_ShouldCreateBrowserWithName()
    {
        // Arrange & Act
        var browser = Browser.FromName("firefox", "1");

        // Assert
        browser.Name.Should().Be("firefox");
        browser.MinVersion.Should().BeNull();
        browser.MaxVersion.Should().BeNull();
        browser.HttpVersion.Should().Be("1");
    }

    [Fact]
    public void HttpBrowserObject_FromString_ShouldParseValidBrowserString()
    {
        // Arrange
        var browserString = "chrome/120.0.6099.71|2";

        // Act
        var browserObject = HttpBrowserObject.FromString(browserString);

        // Assert
        browserObject.Name.Should().Be("chrome");
        browserObject.Version.Should().Equal(120, 0, 6099, 71);
        browserObject.CompleteString.Should().Be(browserString);
        browserObject.HttpVersion.Should().Be("2");
        browserObject.IsHttp2.Should().BeTrue();
        browserObject.MajorVersion.Should().Be(120);
        browserObject.MinorVersion.Should().Be(0);
        browserObject.PatchVersion.Should().Be(6099);
    }

    [Fact]
    public void HttpBrowserObject_FromString_ShouldHandleMissingValueToken()
    {
        // Arrange
        var browserString = "*MISSING_VALUE*|";

        // Act
        var browserObject = HttpBrowserObject.FromString(browserString);

        // Assert
        browserObject.Name.Should().BeNull();
        browserObject.Version.Should().BeEmpty();
        browserObject.CompleteString.Should().Be("*MISSING_VALUE*");
        browserObject.HttpVersion.Should().BeEmpty();
    }

    [Fact]
    public void HttpBrowserObject_FromString_ShouldThrowOnInvalidFormat()
    {
        // Arrange
        var invalidString = "invalid-format";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            HttpBrowserObject.FromString(invalidString));
        
        exception.Message.Should().Contain("Invalid browser string format");
    }

    [Theory]
    [InlineData("chrome/120.0|2", false)]
    [InlineData("chrome/120.0|1", true)]
    public void HttpBrowserObject_IsHttp2_ShouldReturnCorrectValue(string browserString, bool expectedIsHttp1)
    {
        // Arrange & Act
        var browserObject = HttpBrowserObject.FromString(browserString);

        // Assert
        browserObject.IsHttp2.Should().Be(!expectedIsHttp1);
    }

    [Theory]
    [InlineData("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")]
    [InlineData("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")]
    public void HeaderUtils_GetUserAgent_ShouldFindUserAgent(string headerName, string userAgentValue)
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { headerName, userAgentValue }
        };

        // Act
        var result = HeaderUtils.GetUserAgent(headers);

        // Assert
        result.Should().Be(userAgentValue);
    }

    [Fact]
    public void HeaderUtils_GetUserAgent_ShouldReturnNullWhenNotFound()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "Accept", "text/html" }
        };

        // Act
        var result = HeaderUtils.GetUserAgent(headers);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", "chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0", "firefox")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15", "safari")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0", "edge")]
    [InlineData("Some unknown browser", null)]
    public void HeaderUtils_GetBrowser_ShouldIdentifyBrowser(string userAgent, string? expectedBrowser)
    {
        // Act
        var result = HeaderUtils.GetBrowser(userAgent);

        // Assert
        result.Should().Be(expectedBrowser);
    }

    [Theory]
    [InlineData("accept", "Accept")]
    [InlineData("user-agent", "User-Agent")]
    [InlineData("content-type", "Content-Type")]
    [InlineData("sec-ch-ua", "sec-ch-ua")] // Should not be pascalized
    [InlineData(":authority", ":authority")] // Pseudo-header should not be pascalized
    [InlineData("dnt", "DNT")] // Special uppercase header
    [InlineData("rtt", "RTT")] // Special uppercase header
    [InlineData("ect", "ECT")] // Special uppercase header
    public void HeaderUtils_Pascalize_ShouldPascalizeCorrectly(string input, string expected)
    {
        // Act
        var result = HeaderUtils.Pascalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HeaderUtils_PascalizeHeaders_ShouldPascalizeAllHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "user-agent", "Mozilla/5.0" },
            { "accept", "text/html" },
            { "sec-ch-ua", "\"Chrome\";v=\"120\"" }
        };

        // Act
        var result = HeaderUtils.PascalizeHeaders(headers);

        // Assert
        result.Should().ContainKeys("User-Agent", "Accept", "sec-ch-ua");
        result["User-Agent"].Should().Be("Mozilla/5.0");
        result["Accept"].Should().Be("text/html");
        result["sec-ch-ua"].Should().Be("\"Chrome\";v=\"120\"");
    }

    [Theory]
    [InlineData(new[] { "en-US" }, "en-US")]
    [InlineData(new[] { "en-US", "en" }, "en-US, en;q=0.9")]
    [InlineData(new[] { "en-US", "en", "fr" }, "en-US, en;q=0.9, fr;q=0.8")]
    public void HeaderUtils_GenerateAcceptLanguageHeader_ShouldGenerateCorrectHeader(string[] locales, string expected)
    {
        // Act
        var result = HeaderUtils.GenerateAcceptLanguageHeader(locales);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("chrome", 76, true)]
    [InlineData("chrome", 75, false)]
    [InlineData("firefox", 90, true)]
    [InlineData("firefox", 89, false)]
    [InlineData("edge", 79, true)]
    [InlineData("edge", 78, false)]
    [InlineData("safari", 100, false)] // Safari doesn't support Sec-Fetch
    public void HeaderUtils_ShouldAddSecFetch_ShouldReturnCorrectValue(string browserName, int majorVersion, bool expected)
    {
        // Arrange
        var browser = new HttpBrowserObject(browserName, new[] { majorVersion }, "", "2");

        // Act
        var result = HeaderUtils.ShouldAddSecFetch(browser);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HeaderUtils_Tuplify_ShouldHandleVariousInputs()
    {
        // Test with single string - should wrap in array
        var result1 = HeaderUtils.Tuplify("test");
        result1.Should().Equal("test");

        // Test with null
        var result3 = HeaderUtils.Tuplify<string>(null);
        result3.Should().BeEmpty();
    }

    [Fact]
    public void HeaderUtils_TuplifyStrings_ShouldConvertToStrings()
    {
        // Test with string
        var result1 = HeaderUtils.TuplifyStrings("test");
        result1.Should().Equal("test");

        // Test with numbers
        var result2 = HeaderUtils.TuplifyStrings(new[] { 1, 2, 3 });
        result2.Should().Equal("1", "2", "3");

        // Test with null
        var result3 = HeaderUtils.TuplifyStrings(null);
        result3.Should().BeEmpty();
    }

    [Fact]
    public void HeaderGeneratorOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new HeaderGeneratorOptions();

        // Assert
        options.Browsers.Should().BeEquivalentTo(HeaderConstants.SupportedBrowsers);
        options.OperatingSystems.Should().BeEquivalentTo(HeaderConstants.SupportedOperatingSystems);
        options.Devices.Should().BeEquivalentTo(HeaderConstants.SupportedDevices);
        options.Locales.Should().Equal("en-US");
        options.HttpVersion.Should().Be("2");
        options.Strict.Should().BeFalse();
    }

    [Fact]
    public void HeaderGenerationRequestBuilder_ShouldBuildCorrectRequest()
    {
        // Arrange & Act
        var request = new HeaderGenerationRequestBuilder()
            .WithBrowser("chrome")
            .WithOperatingSystem("windows")
            .WithDevice("desktop")
            .WithLocale("en-US")
            .WithHttpVersion("2")
            .WithStrict(true)
            .WithUserAgent("Mozilla/5.0")
            .WithRequestDependentHeader("Referer", "https://example.com")
            .Build();

        // Assert
        request.Browsers.Should().Equal("chrome");
        request.OperatingSystems.Should().Equal("windows");
        request.Devices.Should().Equal("desktop");
        request.Locales.Should().Equal("en-US");
        request.HttpVersion.Should().Be("2");
        request.Strict.Should().BeTrue();
        request.UserAgent.Should().Equal("Mozilla/5.0");
        request.RequestDependentHeaders.Should().ContainKey("Referer");
        request.RequestDependentHeaders!["Referer"].Should().Be("https://example.com");
    }

    [Fact]
    public void HeaderConstants_ShouldHaveExpectedValues()
    {
        // Assert
        HeaderConstants.SupportedBrowsers.Should().Equal("chrome", "firefox", "safari", "edge");
        HeaderConstants.SupportedOperatingSystems.Should().Equal("windows", "macos", "linux", "android", "ios");
        HeaderConstants.SupportedDevices.Should().Equal("desktop", "mobile");
        HeaderConstants.SupportedHttpVersions.Should().Equal("1", "2");
        HeaderConstants.MissingValueDatasetToken.Should().Be("*MISSING_VALUE*");
        
        HeaderConstants.Http1SecFetchAttributes.Should().ContainKeys(
            "Sec-Fetch-Mode", "Sec-Fetch-Dest", "Sec-Fetch-Site", "Sec-Fetch-User");
            
        HeaderConstants.Http2SecFetchAttributes.Should().ContainKeys(
            "sec-fetch-mode", "sec-fetch-dest", "sec-fetch-site", "sec-fetch-user");
            
        HeaderConstants.RelaxationOrder.Should().Equal("locales", "devices", "operatingSystems", "browsers");
        HeaderConstants.PascalizeUpper.Should().Contain("dnt", "rtt", "ect");
    }
}