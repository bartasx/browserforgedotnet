using System.Text.Json.Serialization;

namespace BrowserforgeDotnet.Fingerprints;

/// <summary>
/// Represents the result of fingerprint validation with detailed information about what passed and failed
/// </summary>
/// <param name="IsValid">Whether the fingerprint is overall valid</param>
/// <param name="SuspiciousnessScore">Overall suspiciousness score from 0-100 (higher is more suspicious)</param>
/// <param name="Anomalies">List of detected anomalies</param>
/// <param name="Validations">List of validation checks performed</param>
/// <param name="Details">Additional validation details and context</param>
public record ValidationResult(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("suspiciousnessScore")] int SuspiciousnessScore,
    [property: JsonPropertyName("anomalies")] List<AnomalyReport> Anomalies,
    [property: JsonPropertyName("validations")] List<ValidationCheck> Validations,
    [property: JsonPropertyName("details")] Dictionary<string, object> Details
)
{
    /// <summary>
    /// Creates a validation result indicating a valid fingerprint
    /// </summary>
    /// <param name="suspiciousnessScore">Suspiciousness score</param>
    /// <param name="validations">List of validation checks</param>
    /// <param name="details">Additional details</param>
    /// <returns>Valid ValidationResult</returns>
    public static ValidationResult Valid(int suspiciousnessScore = 0, List<ValidationCheck>? validations = null, Dictionary<string, object>? details = null)
    {
        return new ValidationResult(
            IsValid: true,
            SuspiciousnessScore: suspiciousnessScore,
            Anomalies: new List<AnomalyReport>(),
            Validations: validations ?? new List<ValidationCheck>(),
            Details: details ?? new Dictionary<string, object>()
        );
    }

    /// <summary>
    /// Creates a validation result indicating an invalid fingerprint
    /// </summary>
    /// <param name="anomalies">List of detected anomalies</param>
    /// <param name="suspiciousnessScore">Suspiciousness score</param>
    /// <param name="validations">List of validation checks</param>
    /// <param name="details">Additional details</param>
    /// <returns>Invalid ValidationResult</returns>
    public static ValidationResult Invalid(List<AnomalyReport> anomalies, int suspiciousnessScore = 100, List<ValidationCheck>? validations = null, Dictionary<string, object>? details = null)
    {
        return new ValidationResult(
            IsValid: false,
            SuspiciousnessScore: suspiciousnessScore,
            Anomalies: anomalies,
            Validations: validations ?? new List<ValidationCheck>(),
            Details: details ?? new Dictionary<string, object>()
        );
    }

    /// <summary>
    /// Gets the most critical anomaly (highest severity)
    /// </summary>
    /// <returns>Most critical anomaly or null if none exist</returns>
    public AnomalyReport? GetMostCriticalAnomaly()
    {
        return Anomalies.OrderByDescending(a => (int)a.Severity).FirstOrDefault();
    }

    /// <summary>
    /// Gets anomalies of a specific type
    /// </summary>
    /// <param name="type">Anomaly type to filter by</param>
    /// <returns>List of anomalies of the specified type</returns>
    public List<AnomalyReport> GetAnomaliesByType(AnomalyType type)
    {
        return Anomalies.Where(a => a.Type == type).ToList();
    }

    /// <summary>
    /// Gets the risk assessment based on suspiciousness score
    /// </summary>
    /// <returns>Risk level description</returns>
    public string GetRiskAssessment()
    {
        return SuspiciousnessScore switch
        {
            >= 80 => "Critical Risk - Highly suspicious fingerprint likely to be detected",
            >= 60 => "High Risk - Suspicious patterns detected",
            >= 40 => "Medium Risk - Some inconsistencies found",
            >= 20 => "Low Risk - Minor anomalies detected",
            _ => "Minimal Risk - Fingerprint appears realistic"
        };
    }
}

/// <summary>
/// Represents a specific validation check performed on the fingerprint
/// </summary>
/// <param name="Name">Name of the validation check</param>
/// <param name="Passed">Whether the validation check passed</param>
/// <param name="Description">Description of what was validated</param>
/// <param name="ExpectedValue">Expected value or range</param>
/// <param name="ActualValue">Actual value found</param>
/// <param name="Severity">Severity level if the check failed</param>
public record ValidationCheck(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("expectedValue")] string? ExpectedValue,
    [property: JsonPropertyName("actualValue")] string? ActualValue,
    [property: JsonPropertyName("severity")] AnomalySeverity Severity
);

/// <summary>
/// Represents a detected anomaly in the fingerprint
/// </summary>
/// <param name="Type">Type of anomaly detected</param>
/// <param name="Severity">Severity level of the anomaly</param>
/// <param name="Description">Human-readable description of the anomaly</param>
/// <param name="Field">Field or component where anomaly was detected</param>
/// <param name="ExpectedValue">Expected value or range</param>
/// <param name="ActualValue">Actual suspicious value</param>
/// <param name="SuspiciousnessContribution">How much this anomaly contributes to overall suspiciousness (0-100)</param>
/// <param name="Recommendation">Recommended action to fix the anomaly</param>
public record AnomalyReport(
    [property: JsonPropertyName("type")] AnomalyType Type,
    [property: JsonPropertyName("severity")] AnomalySeverity Severity,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("expectedValue")] string? ExpectedValue,
    [property: JsonPropertyName("actualValue")] string? ActualValue,
    [property: JsonPropertyName("suspiciousnessContribution")] int SuspiciousnessContribution,
    [property: JsonPropertyName("recommendation")] string? Recommendation
);

/// <summary>
/// Types of anomalies that can be detected in fingerprints
/// </summary>
public enum AnomalyType
{
    /// <summary>Browser and platform combination is inconsistent</summary>
    BrowserPlatformInconsistency,
    
    /// <summary>Hardware values are outside realistic ranges</summary>
    UnrealisticHardware,
    
    /// <summary>Screen dimensions don't correlate with device pixel ratio</summary>
    ScreenInconsistency,
    
    /// <summary>Language settings don't match platform</summary>
    LanguageInconsistency,
    
    /// <summary>Codec support doesn't match browser type</summary>
    CodecInconsistency,
    
    /// <summary>Navigator properties don't match User-Agent</summary>
    NavigatorInconsistency,
    
    /// <summary>Font availability doesn't match platform</summary>
    FontInconsistency,
    
    /// <summary>Timing patterns appear suspicious</summary>
    SuspiciousTiming,
    
    /// <summary>Statistical outlier detected</summary>
    StatisticalOutlier,
    
    /// <summary>Hardware combination is impossible</summary>
    ImpossibleHardware,
    
    /// <summary>Fingerprint is too perfect/common</summary>
    TooCommon,
    
    /// <summary>Pattern suggests automated generation</summary>
    AutomatedGeneration,
    
    /// <summary>Battery information is unrealistic</summary>
    UnrealisticBattery,
    
    /// <summary>WebRTC or multimedia device inconsistency</summary>
    MultimediaInconsistency
}

/// <summary>
/// Severity levels for anomalies
/// </summary>
public enum AnomalySeverity
{
    /// <summary>Minor issue that slightly increases suspiciousness</summary>
    Low = 1,
    
    /// <summary>Moderate issue that significantly increases suspiciousness</summary>
    Medium = 2,
    
    /// <summary>Major issue that greatly increases suspiciousness</summary>
    High = 3,
    
    /// <summary>Critical issue that makes fingerprint highly suspicious</summary>
    Critical = 4
}

/// <summary>
/// Represents a detailed suspiciousness score breakdown
/// </summary>
/// <param name="OverallScore">Overall suspiciousness score (0-100)</param>
/// <param name="ComponentScores">Score breakdown by component</param>
/// <param name="RiskFactors">List of identified risk factors</param>
/// <param name="Recommendations">List of recommendations to improve the fingerprint</param>
public record SuspiciousnessScore(
    [property: JsonPropertyName("overallScore")] int OverallScore,
    [property: JsonPropertyName("componentScores")] Dictionary<string, int> ComponentScores,
    [property: JsonPropertyName("riskFactors")] List<string> RiskFactors,
    [property: JsonPropertyName("recommendations")] List<string> Recommendations
);