using System.IO.Compression;
using System.Text.Json;

namespace BrowserforgeDotnet.BayesianNetwork;

/// <summary>
/// Utility functions for Bayesian Network operations
/// </summary>
public static class NetworkUtilities
{
    /// <summary>
    /// Performs a set "intersection" on the given (flat) arrays
    /// </summary>
    /// <typeparam name="T">Type of elements in the sequences</typeparam>
    /// <param name="a">First sequence</param>
    /// <param name="b">Second sequence</param>
    /// <returns>List containing elements present in both sequences</returns>
    public static List<T> ArrayIntersection<T>(IEnumerable<T> a, IEnumerable<T> b)
    {
        var setB = new HashSet<T>(b);
        return a.Where(x => setB.Contains(x)).ToList();
    }

    /// <summary>
    /// Combines two arrays into a single array using the set union
    /// </summary>
    /// <typeparam name="T">Type of elements in the tuples</typeparam>
    /// <param name="a">First array to be combined</param>
    /// <param name="b">Second array to be combined</param>
    /// <returns>Zipped (multi-dimensional) array</returns>
    public static List<HashSet<T>> ArrayZip<T>(IReadOnlyList<HashSet<T>> a, IReadOnlyList<HashSet<T>> b)
    {
        var result = new List<HashSet<T>>();
        var minLength = Math.Min(a.Count, b.Count);
        
        for (int i = 0; i < minLength; i++)
        {
            var unionSet = new HashSet<T>(a[i]);
            unionSet.UnionWith(b[i]);
            result.Add(unionSet);
        }
        
        return result;
    }

    /// <summary>
    /// Removes the "deeper/skip" structures from the conditional probability table
    /// </summary>
    /// <param name="obj">Object to process (typically a JsonElement)</param>
    /// <returns>Processed dictionary without deeper/skip structures</returns>
    public static Dictionary<string, object> Undeeper(object obj)
    {
        if (obj is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object>();
        }

        var result = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            switch (property.Name)
            {
                case "skip":
                    continue; // Skip this property
                case "deeper":
                    var deeperResult = Undeeper(property.Value);
                    foreach (var kvp in deeperResult)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                    break;
                default:
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        result[property.Name] = Undeeper(property.Value);
                    }
                    else
                    {
                        result[property.Name] = ExtractJsonValue(property.Value);
                    }
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Performs DFS on the Tree and returns values of the nodes on the paths that end with the given keys
    /// (stored by levels - first level is the root)
    /// </summary>
    /// <param name="tree">Tree structure to search</param>
    /// <param name="validKeys">Keys to search for at leaf nodes</param>
    /// <returns>List of sets representing paths that end with valid keys</returns>
    public static List<HashSet<string>> FilterByLastLevelKeys(Dictionary<string, object> tree, IEnumerable<string> validKeys)
    {
        var validKeySet = new HashSet<string>(validKeys);
        var result = new List<HashSet<string>>();

        void Recurse(Dictionary<string, object> currentTree, List<string> accumulator)
        {
            foreach (var kvp in currentTree)
            {
                if (kvp.Value is not Dictionary<string, object> nestedDict)
                {
                    // Leaf node
                    if (validKeySet.Contains(kvp.Key))
                    {
                        if (result.Count == 0)
                        {
                            // Initialize result with single-element sets
                            result.AddRange(accumulator.Select(x => new HashSet<string> { x }));
                        }
                        else
                        {
                            // Zip with existing result
                            var newSets = accumulator.Select(x => new HashSet<string> { x }).ToList();
                            result = ArrayZip(result, newSets);
                        }
                    }
                }
                else
                {
                    // Continue recursion
                    var newAccumulator = new List<string>(accumulator) { kvp.Key };
                    Recurse(nestedDict, newAccumulator);
                }
            }
        }

        Recurse(tree, new List<string>());
        return result;
    }

    /// <summary>
    /// Given a bayesian network instance and a set of user constraints, returns an extended
    /// set of constraints induced by the original constraints and network structure
    /// </summary>
    /// <param name="network">The Bayesian network instance</param>
    /// <param name="possibleValues">Dictionary of constraints on possible values</param>
    /// <returns>Extended set of constraints induced by the network structure</returns>
    public static Dictionary<string, IEnumerable<string>> GetPossibleValues(
        BrowserforgeDotnet.BayesianNetwork.BayesianNetwork network,
        Dictionary<string, IEnumerable<string>> possibleValues)
    {
        var sets = new List<Dictionary<string, IEnumerable<string>>>();

        // For every pre-specified node, compute the "closure" for values of the other nodes
        foreach (var kvp in possibleValues)
        {
            var key = kvp.Key;
            var value = kvp.Value.ToList();

            if (value.Count == 0)
            {
                throw new InvalidOperationException(
                    "The current constraints are too restrictive. No possible values can be found for the given constraints.");
            }

            if (!network.NodesByName.TryGetValue(key, out var node))
            {
                continue;
            }

            var nodeDefinition = node.GetType().GetField("_nodeDefinition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nodeDefinition?.GetValue(node) is not Dictionary<string, object> nodeDef ||
                !nodeDef.TryGetValue("conditionalProbabilities", out var conditionalProbs))
            {
                continue;
            }

            var tree = Undeeper(conditionalProbs);
            var zippedValues = FilterByLastLevelKeys(tree, value);
            
            var setDict = new Dictionary<string, IEnumerable<string>> { [key] = value };
            
            // Add parent values from zipped results
            var parentNames = node.ParentNames;
            for (int i = 0; i < Math.Min(parentNames.Count, zippedValues.Count); i++)
            {
                setDict[parentNames[i]] = zippedValues[i];
            }
            
            sets.Add(setDict);
        }

        // Compute the intersection of all the possible values for each node
        var result = new Dictionary<string, IEnumerable<string>>();
        
        foreach (var setDict in sets)
        {
            foreach (var kvp in setDict)
            {
                var key = kvp.Key;
                var values = kvp.Value;
                
                if (result.ContainsKey(key))
                {
                    var intersectedValues = ArrayIntersection(values, result[key]).ToList();
                    if (intersectedValues.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "The current constraints are too restrictive. No possible values can be found for the given constraints.");
                    }
                    result[key] = intersectedValues;
                }
                else
                {
                    result[key] = values;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Unzips a zip file if the path points to a zip file, otherwise directly loads a JSON file
    /// </summary>
    /// <param name="path">The path to the zip file or JSON file</param>
    /// <returns>A dictionary representing the JSON content</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
    /// <exception cref="JsonException">Thrown when JSON parsing fails</exception>
    public static async Task<Dictionary<string, object>> ExtractJsonAsync(string path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        try
        {
            if (Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Handle ZIP file
                using var archive = ZipFile.OpenRead(path);
                var jsonEntry = archive.Entries.FirstOrDefault(e => 
                    Path.GetExtension(e.Name).Equals(".json", StringComparison.OrdinalIgnoreCase));
                
                if (jsonEntry == null)
                {
                    return new Dictionary<string, object>(); // Broken - no JSON file found
                }

                using var stream = jsonEntry.Open();
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();
                
                var jsonDocument = JsonDocument.Parse(jsonContent);
                return ConvertJsonElementToDictionary(jsonDocument.RootElement);
            }
            else
            {
                // Handle direct JSON file
                var jsonContent = await File.ReadAllTextAsync(path);
                var jsonDocument = JsonDocument.Parse(jsonContent);
                return ConvertJsonElementToDictionary(jsonDocument.RootElement);
            }
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract JSON from {path}", ex);
        }
    }

    /// <summary>
    /// Synchronous version of ExtractJsonAsync for compatibility
    /// </summary>
    /// <param name="path">The path to the zip file or JSON file</param>
    /// <returns>A dictionary representing the JSON content</returns>
    public static Dictionary<string, object> ExtractJson(string path)
    {
        return ExtractJsonAsync(path).GetAwaiter().GetResult();
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