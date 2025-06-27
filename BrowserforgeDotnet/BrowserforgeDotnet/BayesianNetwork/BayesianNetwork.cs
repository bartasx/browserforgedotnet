using System.Text.Json;

namespace BrowserforgeDotnet.BayesianNetwork;

/// <summary>
/// Implementation of a bayesian network capable of randomly sampling from its distribution
/// </summary>
public class BayesianNetwork
{
    private readonly List<BayesianNode> _nodesInSamplingOrder;
    private readonly Dictionary<string, BayesianNode> _nodesByName;

    /// <summary>
    /// Gets the nodes in their sampling order (topologically sorted)
    /// </summary>
    public IReadOnlyList<BayesianNode> NodesInSamplingOrder => _nodesInSamplingOrder;

    /// <summary>
    /// Gets nodes indexed by their names for quick lookup
    /// </summary>
    public IReadOnlyDictionary<string, BayesianNode> NodesByName => _nodesByName;

    /// <summary>
    /// Initializes a new instance of the BayesianNetwork class from a file path
    /// </summary>
    /// <param name="path">Path to the network definition file (JSON or ZIP)</param>
    public BayesianNetwork(string path)
    {
        var networkDefinition = NetworkUtilities.ExtractJson(path);
        
        if (!networkDefinition.TryGetValue("nodes", out var nodesObj) || 
            nodesObj is not JsonElement nodesElement)
        {
            throw new ArgumentException("Invalid network definition: missing 'nodes' array", nameof(path));
        }

        _nodesInSamplingOrder = new List<BayesianNode>();
        _nodesByName = new Dictionary<string, BayesianNode>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            var nodeDefinition = ConvertJsonElementToDictionary(nodeElement);
            var node = new BayesianNode(nodeDefinition);
            
            _nodesInSamplingOrder.Add(node);
            _nodesByName[node.Name] = node;
        }
    }

    /// <summary>
    /// Initializes a new instance of the BayesianNetwork class from a network definition
    /// </summary>
    /// <param name="networkDefinition">Dictionary containing the network definition</param>
    public BayesianNetwork(Dictionary<string, object> networkDefinition)
    {
        _nodesInSamplingOrder = new List<BayesianNode>();
        _nodesByName = new Dictionary<string, BayesianNode>();

        if (!networkDefinition.TryGetValue("nodes", out var nodesObj))
        {
            throw new ArgumentException("Invalid network definition: missing 'nodes' array", nameof(networkDefinition));
        }

        // Handle both JsonElement and List<object> formats
        if (nodesObj is JsonElement nodesElement && nodesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var nodeElement in nodesElement.EnumerateArray())
            {
                var nodeDefinition = ConvertJsonElementToDictionary(nodeElement);
                var node = new BayesianNode(nodeDefinition);
                
                _nodesInSamplingOrder.Add(node);
                _nodesByName[node.Name] = node;
            }
        }
        else if (nodesObj is List<object> nodesList)
        {
            foreach (var nodeObj in nodesList)
            {
                Dictionary<string, object> nodeDefinition;
                
                if (nodeObj is JsonElement nodeElement)
                {
                    nodeDefinition = ConvertJsonElementToDictionary(nodeElement);
                }
                else if (nodeObj is Dictionary<string, object> nodeDict)
                {
                    nodeDefinition = nodeDict;
                }
                else
                {
                    continue; // Skip invalid node definitions
                }
                
                var node = new BayesianNode(nodeDefinition);
                _nodesInSamplingOrder.Add(node);
                _nodesByName[node.Name] = node;
            }
        }
        else
        {
            throw new ArgumentException("Invalid network definition: 'nodes' must be an array", nameof(networkDefinition));
        }
    }

    /// <summary>
    /// Randomly samples from the distribution represented by the bayesian network
    /// </summary>
    /// <param name="inputValues">Optional pre-specified values for some nodes</param>
    /// <returns>Complete sample with values for all nodes</returns>
    public Dictionary<string, object> GenerateSample(Dictionary<string, object>? inputValues = null)
    {
        var sample = inputValues?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>();

        foreach (var node in _nodesInSamplingOrder)
        {
            if (!sample.ContainsKey(node.Name))
            {
                sample[node.Name] = node.Sample(sample);
            }
        }

        return sample;
    }

    /// <summary>
    /// Randomly samples values from the distribution represented by the bayesian network,
    /// making sure the sample is consistent with the provided restrictions on value possibilities.
    /// Returns null if no such sample can be generated.
    /// </summary>
    /// <param name="valuePossibilities">Dictionary specifying allowed values for each node</param>
    /// <returns>Consistent sample or null if impossible</returns>
    public Dictionary<string, object>? GenerateConsistentSampleWhenPossible(
        Dictionary<string, IEnumerable<string>> valuePossibilities)
    {
        return RecursivelyGenerateConsistentSampleWhenPossible(
            new Dictionary<string, object>(), 
            valuePossibilities, 
            0);
    }

    /// <summary>
    /// Recursively generates a random sample consistent with the given restrictions on possible values
    /// </summary>
    /// <param name="sampleSoFar">Partial sample built so far</param>
    /// <param name="valuePossibilities">Dictionary specifying allowed values for each node</param>
    /// <param name="depth">Current depth in the sampling process</param>
    /// <returns>Complete consistent sample or null if impossible</returns>
    private Dictionary<string, object>? RecursivelyGenerateConsistentSampleWhenPossible(
        Dictionary<string, object> sampleSoFar,
        Dictionary<string, IEnumerable<string>> valuePossibilities,
        int depth)
    {
        if (depth == _nodesInSamplingOrder.Count)
        {
            return sampleSoFar;
        }

        var node = _nodesInSamplingOrder[depth];
        var bannedValues = new List<string>();
        
        while (true)
        {
            var possibleValues = valuePossibilities.TryGetValue(node.Name, out var nodeValues) 
                ? nodeValues 
                : node.PossibleValues;

            var sampleValue = node.SampleAccordingToRestrictions(
                sampleSoFar,
                possibleValues,
                bannedValues);

            if (sampleValue == null)
            {
                break; // No valid value found
            }

            // Try this value
            sampleSoFar[node.Name] = sampleValue;
            
            var nextSample = RecursivelyGenerateConsistentSampleWhenPossible(
                sampleSoFar, 
                valuePossibilities, 
                depth + 1);

            if (nextSample != null)
            {
                return nextSample; // Success!
            }

            // Backtrack
            bannedValues.Add(sampleValue);
            sampleSoFar.Remove(node.Name);
        }

        return null; // No consistent sample found
    }

    /// <summary>
    /// Gets all possible values for nodes given constraints, using constraint propagation
    /// </summary>
    /// <param name="possibleValues">Initial constraints on node values</param>
    /// <returns>Extended constraints induced by network structure</returns>
    public Dictionary<string, IEnumerable<string>> GetPossibleValues(
        Dictionary<string, IEnumerable<string>> possibleValues)
    {
        return NetworkUtilities.GetPossibleValues(this, possibleValues);
    }

    /// <summary>
    /// Converts a JsonElement to a Dictionary recursively
    /// </summary>
    /// <param name="element">JsonElement to convert</param>
    /// <returns>Dictionary representation of the JsonElement</returns>
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

    /// <summary>
    /// Extracts the appropriate .NET value from a JsonElement
    /// </summary>
    /// <param name="element">JsonElement to extract value from</param>
    /// <returns>Appropriate .NET object representing the JSON value</returns>
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