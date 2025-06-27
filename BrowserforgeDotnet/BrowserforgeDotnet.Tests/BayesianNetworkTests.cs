using System.Text.Json;
using BrowserforgeDotnet.BayesianNetwork;
using Xunit;
using FluentAssertions;

namespace BrowserforgeDotnet.Tests;

/// <summary>
/// Tests for the Bayesian Network implementation
/// </summary>
public class BayesianNetworkTests
{
    /// <summary>
    /// Creates a simple test network definition for testing
    /// </summary>
    private static Dictionary<string, object> CreateTestNetworkDefinition()
    {
        var json = """
        {
          "nodes": [
            {
              "name": "A",
              "possibleValues": ["a1", "a2"],
              "parentNames": [],
              "conditionalProbabilities": {
                "a1": 0.6,
                "a2": 0.4
              }
            },
            {
              "name": "B",
              "possibleValues": ["b1", "b2"],
              "parentNames": ["A"],
              "conditionalProbabilities": {
                "deeper": {
                  "a1": {
                    "b1": 0.8,
                    "b2": 0.2
                  },
                  "a2": {
                    "b1": 0.3,
                    "b2": 0.7
                  }
                },
                "skip": {
                  "b1": 0.5,
                  "b2": 0.5
                }
              }
            }
          ]
        }
        """;

        using var document = JsonDocument.Parse(json);
        return ConvertJsonElementToDictionary(document.RootElement);
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

    [Fact]
    public void BayesianNode_Should_Initialize_Correctly()
    {
        // Arrange
        var nodeDefinition = new Dictionary<string, object>
        {
            ["name"] = "TestNode",
            ["possibleValues"] = JsonDocument.Parse("""["value1", "value2"]""").RootElement,
            ["parentNames"] = JsonDocument.Parse("""["parent1"]""").RootElement
        };

        // Act
        var node = new BayesianNode(nodeDefinition);

        // Assert
        node.Name.Should().Be("TestNode");
        node.PossibleValues.Should().BeEquivalentTo(new[] { "value1", "value2" });
        node.ParentNames.Should().BeEquivalentTo(new[] { "parent1" });
    }

    [Fact]
    public void BayesianNode_Should_Sample_From_Unconditional_Distribution()
    {
        // Arrange
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
        var node = new BayesianNode(nodeDefinition, new Random(42)); // Fixed seed for reproducibility

        // Act
        var samples = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            samples.Add(node.Sample(new Dictionary<string, object>()));
        }

        // Assert
        var a1Count = samples.Count(s => s == "a1");
        var a2Count = samples.Count(s => s == "a2");
        
        // With 1000 samples, we expect roughly 700 a1 and 300 a2 (allow some variance)
        a1Count.Should().BeInRange(650, 750);
        a2Count.Should().BeInRange(250, 350);
    }

    [Fact]
    public void BayesianNode_Should_Sample_From_Conditional_Distribution()
    {
        // Arrange
        var json = """
        {
          "name": "B",
          "possibleValues": ["b1", "b2"],
          "parentNames": ["A"],
          "conditionalProbabilities": {
            "deeper": {
              "a1": {
                "b1": 0.8,
                "b2": 0.2
              },
              "a2": {
                "b1": 0.3,
                "b2": 0.7
              }
            }
          }
        }
        """;

        using var document = JsonDocument.Parse(json);
        var nodeDefinition = ConvertJsonElementToDictionary(document.RootElement);
        var node = new BayesianNode(nodeDefinition, new Random(42));

        // Act - sample given A = "a1"
        var samplesGivenA1 = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            samplesGivenA1.Add(node.Sample(new Dictionary<string, object> { ["A"] = "a1" }));
        }

        // Act - sample given A = "a2"
        var samplesGivenA2 = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            samplesGivenA2.Add(node.Sample(new Dictionary<string, object> { ["A"] = "a2" }));
        }

        // Assert
        var b1CountGivenA1 = samplesGivenA1.Count(s => s == "b1");
        var b1CountGivenA2 = samplesGivenA2.Count(s => s == "b1");

        // Given A=a1, expect ~80% b1; given A=a2, expect ~30% b1
        b1CountGivenA1.Should().BeInRange(750, 850);
        b1CountGivenA2.Should().BeInRange(250, 350);
    }

    [Fact]
    public void BayesianNetwork_Should_Initialize_Correctly()
    {
        // Arrange
        var networkDefinition = CreateTestNetworkDefinition();

        // Act
        var network = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(networkDefinition);

        // Assert
        network.NodesInSamplingOrder.Should().HaveCount(2);
        network.NodesByName.Should().ContainKey("A");
        network.NodesByName.Should().ContainKey("B");
        network.NodesByName["A"].Name.Should().Be("A");
        network.NodesByName["B"].Name.Should().Be("B");
    }

    [Fact]
    public void BayesianNetwork_Should_Generate_Complete_Sample()
    {
        // Arrange
        var networkDefinition = CreateTestNetworkDefinition();
        var network = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(networkDefinition);

        // Act
        var sample = network.GenerateSample();

        // Assert
        sample.Should().ContainKey("A");
        sample.Should().ContainKey("B");
        sample["A"].Should().BeOneOf("a1", "a2");
        sample["B"].Should().BeOneOf("b1", "b2");
    }

    [Fact]
    public void BayesianNetwork_Should_Generate_Sample_With_Input_Values()
    {
        // Arrange
        var networkDefinition = CreateTestNetworkDefinition();
        var network = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(networkDefinition);
        var inputValues = new Dictionary<string, object> { ["A"] = "a1" };

        // Act
        var sample = network.GenerateSample(inputValues);

        // Assert
        sample.Should().ContainKey("A");
        sample.Should().ContainKey("B");
        sample["A"].Should().Be("a1");
        sample["B"].Should().BeOneOf("b1", "b2");
    }

    [Fact]
    public void BayesianNetwork_Should_Generate_Consistent_Sample_When_Possible()
    {
        // Arrange
        var networkDefinition = CreateTestNetworkDefinition();
        var network = new BrowserforgeDotnet.BayesianNetwork.BayesianNetwork(networkDefinition);
        var valuePossibilities = new Dictionary<string, IEnumerable<string>>
        {
            ["A"] = new[] { "a1" },
            ["B"] = new[] { "b1", "b2" }
        };

        // Act
        var sample = network.GenerateConsistentSampleWhenPossible(valuePossibilities);

        // Assert
        sample.Should().NotBeNull();
        sample!["A"].Should().Be("a1");
        sample["B"].Should().BeOneOf("b1", "b2");
    }

    [Fact]
    public void NetworkUtilities_ArrayIntersection_Should_Work_Correctly()
    {
        // Arrange
        var a = new[] { "1", "2", "3", "4" };
        var b = new[] { "3", "4", "5", "6" };

        // Act
        var result = NetworkUtilities.ArrayIntersection(a, b);

        // Assert
        result.Should().BeEquivalentTo(new[] { "3", "4" });
    }

    [Fact]
    public void NetworkUtilities_ArrayZip_Should_Work_Correctly()
    {
        // Arrange
        var a = new List<HashSet<string>>
        {
            new() { "a1", "a2" },
            new() { "b1" }
        };
        var b = new List<HashSet<string>>
        {
            new() { "a2", "a3" },
            new() { "b1", "b2" }
        };

        // Act
        var result = NetworkUtilities.ArrayZip(a, b);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new[] { "a1", "a2", "a3" });
        result[1].Should().BeEquivalentTo(new[] { "b1", "b2" });
    }
}