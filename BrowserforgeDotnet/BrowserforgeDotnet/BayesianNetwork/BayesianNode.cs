using System.Text.Json;

namespace BrowserforgeDotnet.BayesianNetwork;

/// <summary>
/// Implementation of a single node in a bayesian network allowing sampling from its conditional distribution
/// </summary>
public class BayesianNode
{
    private readonly Dictionary<string, object> _nodeDefinition;
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the BayesianNode class
    /// </summary>
    /// <param name="nodeDefinition">The node definition containing conditional probabilities and metadata</param>
    public BayesianNode(Dictionary<string, object> nodeDefinition)
    {
        _nodeDefinition = nodeDefinition ?? throw new ArgumentNullException(nameof(nodeDefinition));
        _random = new Random();
    }

    /// <summary>
    /// Initializes a new instance of the BayesianNode class with a custom random instance
    /// </summary>
    /// <param name="nodeDefinition">The node definition containing conditional probabilities and metadata</param>
    /// <param name="random">Custom random instance for testing/reproducibility</param>
    public BayesianNode(Dictionary<string, object> nodeDefinition, Random random)
    {
        _nodeDefinition = nodeDefinition ?? throw new ArgumentNullException(nameof(nodeDefinition));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// Gets the name of this node
    /// </summary>
    public string Name => _nodeDefinition.TryGetValue("name", out var name) ? name.ToString()! : string.Empty;

    /// <summary>
    /// Gets the names of parent nodes that this node depends on
    /// </summary>
    public IReadOnlyList<string> ParentNames
    {
        get
        {
            if (_nodeDefinition.TryGetValue("parentNames", out var parentNames))
            {
                if (parentNames is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(x => x.GetString()!)
                        .Where(x => x != null)
                        .ToList();
                }
                else if (parentNames is List<object> list)
                {
                    return list.Select(x => x?.ToString()!)
                        .Where(x => x != null)
                        .ToList();
                }
            }
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets all possible values that this node can take
    /// </summary>
    public IReadOnlyList<string> PossibleValues
    {
        get
        {
            if (_nodeDefinition.TryGetValue("possibleValues", out var possibleValues))
            {
                if (possibleValues is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(x => x.GetString()!)
                        .Where(x => x != null)
                        .ToList();
                }
                else if (possibleValues is List<object> list)
                {
                    return list.Select(x => x?.ToString()!)
                        .Where(x => x != null)
                        .ToList();
                }
            }
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Extracts unconditional probabilities of node values given the values of the parent nodes
    /// </summary>
    /// <param name="parentValues">Dictionary containing values of parent nodes</param>
    /// <returns>Dictionary mapping possible values to their probabilities</returns>
    public Dictionary<string, double> GetProbabilitiesGivenKnownValues(Dictionary<string, object> parentValues)
    {
        if (!_nodeDefinition.TryGetValue("conditionalProbabilities", out var conditionalProbabilitiesObj))
        {
            return new Dictionary<string, double>();
        }

        var probabilities = conditionalProbabilitiesObj;

        foreach (var parentName in ParentNames)
        {
            if (parentValues.TryGetValue(parentName, out var parentValue))
            {
                var parentValueStr = parentValue?.ToString();
                
                // Handle different probability structure formats
                if (probabilities is JsonElement probElement)
                {
                    if (probElement.TryGetProperty("deeper", out var deeperElement) &&
                        deeperElement.TryGetProperty(parentValueStr!, out var nextElement))
                    {
                        probabilities = nextElement;
                    }
                    else if (probElement.TryGetProperty("skip", out var skipElement))
                    {
                        probabilities = skipElement;
                    }
                    else
                    {
                        return new Dictionary<string, double>();
                    }
                }
                else if (probabilities is Dictionary<string, object> probDict)
                {
                    if (probDict.TryGetValue("deeper", out var deeperObj) &&
                        deeperObj is Dictionary<string, object> deeperDict &&
                        deeperDict.TryGetValue(parentValueStr!, out var nextObj))
                    {
                        probabilities = nextObj;
                    }
                    else if (probDict.TryGetValue("skip", out var skipObj))
                    {
                        probabilities = skipObj;
                    }
                    else
                    {
                        return new Dictionary<string, double>();
                    }
                }
                else
                {
                    return new Dictionary<string, double>();
                }
            }
        }

        return ExtractProbabilityDictionary(probabilities);
    }

    /// <summary>
    /// Randomly samples from the given values using the given probabilities
    /// </summary>
    /// <param name="possibleValues">List of possible values to sample from</param>
    /// <param name="probabilities">Dictionary mapping values to their probabilities</param>
    /// <returns>A randomly sampled value</returns>
    public string SampleRandomValueFromPossibilities(IReadOnlyList<string> possibleValues, Dictionary<string, double> probabilities)
    {
        if (possibleValues.Count == 0)
            return string.Empty;

        // Fast weighted random sampling - much faster than standard approaches
        var anchor = _random.NextDouble();
        var cumulativeProbability = 0.0;

        foreach (var possibleValue in possibleValues)
        {
            if (probabilities.TryGetValue(possibleValue, out var probability))
            {
                cumulativeProbability += probability;
                if (cumulativeProbability > anchor)
                {
                    return possibleValue;
                }
            }
        }

        // Default to first item
        return possibleValues[0];
    }

    /// <summary>
    /// Randomly samples from the conditional distribution of this node given values of parents
    /// </summary>
    /// <param name="parentValues">Dictionary containing values of parent nodes</param>
    /// <returns>A randomly sampled value for this node</returns>
    public string Sample(Dictionary<string, object> parentValues)
    {
        var probabilities = GetProbabilitiesGivenKnownValues(parentValues);
        var possibleValues = probabilities.Keys.ToList();
        return SampleRandomValueFromPossibilities(possibleValues, probabilities);
    }

    /// <summary>
    /// Randomly samples from the conditional distribution of this node given restrictions on the possible values and the values of the parents
    /// </summary>
    /// <param name="parentValues">Dictionary containing values of parent nodes</param>
    /// <param name="valuePossibilities">Enumerable of values that are allowed for this node</param>
    /// <param name="bannedValues">List of values that are explicitly banned</param>
    /// <returns>A randomly sampled value that satisfies all constraints, or null if no valid value exists</returns>
    public string? SampleAccordingToRestrictions(
        Dictionary<string, object> parentValues,
        IEnumerable<string> valuePossibilities,
        IReadOnlyList<string> bannedValues)
    {
        var probabilities = GetProbabilitiesGivenKnownValues(parentValues);
        var bannedSet = new HashSet<string>(bannedValues);
        
        var validValues = valuePossibilities
            .Where(value => !bannedSet.Contains(value) && probabilities.ContainsKey(value))
            .ToList();

        if (validValues.Count > 0)
        {
            return SampleRandomValueFromPossibilities(validValues, probabilities);
        }

        return null; // Equivalent to `false` in TypeScript
    }

    /// <summary>
    /// Extracts probability dictionary from a JsonElement or other object
    /// </summary>
    /// <param name="probabilities">The probabilities object from JSON</param>
    /// <returns>Dictionary mapping string values to double probabilities</returns>
    private static Dictionary<string, double> ExtractProbabilityDictionary(object probabilities)
    {
        var result = new Dictionary<string, double>();

        if (probabilities is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name != "deeper" && property.Name != "skip" &&
                    property.Value.TryGetDouble(out var doubleValue))
                {
                    result[property.Name] = doubleValue;
                }
            }
        }
        else if (probabilities is Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key != "deeper" && kvp.Key != "skip")
                {
                    if (kvp.Value is double doubleVal)
                    {
                        result[kvp.Key] = doubleVal;
                    }
                    else if (kvp.Value is int intVal)
                    {
                        result[kvp.Key] = intVal;
                    }
                    else if (double.TryParse(kvp.Value?.ToString(), out var parsedDouble))
                    {
                        result[kvp.Key] = parsedDouble;
                    }
                }
            }
        }

        return result;
    }
}