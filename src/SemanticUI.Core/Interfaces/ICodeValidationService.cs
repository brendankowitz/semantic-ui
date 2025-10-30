namespace SemanticUI.Core.Interfaces;

public interface ICodeValidationService
{
    ValidationResult ValidateReactCode(string code);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<SecurityViolation> Violations { get; set; } = new();
}

public class SecurityViolation
{
    public ViolationType Type { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Severity Severity { get; set; }
}

public enum ViolationType
{
    DangerousIdentifier,
    EvalCall,
    ParseError,
    SuspiciousPattern
}

public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}
