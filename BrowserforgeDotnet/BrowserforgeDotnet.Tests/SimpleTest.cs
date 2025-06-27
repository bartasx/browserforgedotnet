using System.Text.Json;
using BrowserforgeDotnet.BayesianNetwork;
using Xunit;
using Xunit.Abstractions;

namespace BrowserforgeDotnet.Tests;

public class SimpleTest
{
    private readonly ITestOutputHelper _output;

    public SimpleTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_Simple_Node_Sampling()
    {
        // Create a very simple node definition
        var json = """
        {
          "name": "A",
          "possibleValues": ["a1", "a2"],
          "parentNames": [],
          "conditionalProbabilities": {
            "a1": 0.7,
            "a2": 0.3
          }
        }
        """;

        using var document = JsonDocument.Parse(json);
        var nodeDefinition = ConvertJsonElementToDictionary(document.RootElement);
        var node = new BayesianNode(nodeDefinition, new Random(42));

        // Test probability extraction
        var probs = node.GetProbabilitiesGivenKnownValues(new Dictionary<string, object>());
        
        // Debug output
        _output.WriteLine($"Node name: {node.Name}");
        _output.WriteLine($"Possible values: {string.Join(", ", node.PossibleValues)}");
        _output.WriteLine($"Parent names: {string.Join(", ", node.ParentNames)}");
        _output.WriteLine($"Probabilities count: {probs.Count}");
        foreach (var kvp in probs)
        {
            _output.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        // Test sampling
        var sample = node.Sample(new Dictionary<string, object>());
        _output.WriteLine($"Sample result: '{sample}'");
        
        // Assertions
        Assert.True(probs.Count > 0, "Should have probabilities");
        Assert.True(!string.IsNullOrEmpty(sample), "Sample should not be empty");
    }

    private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object>();
        
        if (element.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ExtractJsonValue(property.Value);
        }

        return result;
    }

    private static object ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToList(),
            _ => element.GetRawText()
        };
    }
}