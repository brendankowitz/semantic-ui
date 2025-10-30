using Esprima;
using Esprima.Ast;
using SemanticUI.Core.Interfaces;

namespace SemanticUI.Api.Security;

public class CodeValidationService : ICodeValidationService
{
    private static readonly string[] DangerousIdentifiers = new[]
    {
        "eval", "Function", "setTimeout", "setInterval",
        "document.write", "innerHTML", "outerHTML",
        "__proto__", "constructor.prototype", "XMLHttpRequest",
        "fetch", "import", "require"
    };

    private readonly ILogger<CodeValidationService> _logger;

    public CodeValidationService(ILogger<CodeValidationService> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateReactCode(string code)
    {
        var violations = new List<SecurityViolation>();

        try
        {
            var parser = new JavaScriptParser();
            var ast = parser.ParseScript(code, new ParserOptions
            {
                Tolerant = false,
                Loc = true
            });

            // Walk the AST
            var visitor = new SecurityVisitor(violations, _logger);
            ast.AcceptVisitor(visitor);

            if (violations.Any(v => v.Severity == Severity.Critical))
            {
                _logger.LogWarning("Code validation failed with critical violations: {Violations}",
                    string.Join(", ", violations.Select(v => v.Message)));
            }

            return new ValidationResult
            {
                IsValid = violations.Count == 0,
                Violations = violations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse code for validation");

            violations.Add(new SecurityViolation
            {
                Type = ViolationType.ParseError,
                Message = $"Failed to parse code: {ex.Message}",
                Severity = Severity.Critical
            });

            return new ValidationResult
            {
                IsValid = false,
                Violations = violations
            };
        }
    }

    private class SecurityVisitor : AstVisitor
    {
        private readonly List<SecurityViolation> _violations;
        private readonly ILogger _logger;

        public SecurityVisitor(List<SecurityViolation> violations, ILogger logger)
        {
            _violations = violations;
            _logger = logger;
        }

        protected override object? VisitIdentifier(Identifier identifier)
        {
            if (DangerousIdentifiers.Contains(identifier.Name))
            {
                _violations.Add(new SecurityViolation
                {
                    Type = ViolationType.DangerousIdentifier,
                    Pattern = identifier.Name,
                    Message = $"Dangerous identifier detected: {identifier.Name}",
                    Severity = identifier.Name == "eval" ? Severity.Critical : Severity.High
                });
            }
            return base.VisitIdentifier(identifier);
        }

        protected override object? VisitCallExpression(CallExpression callExpression)
        {
            if (callExpression.Callee is Identifier id)
            {
                if (id.Name == "eval")
                {
                    _violations.Add(new SecurityViolation
                    {
                        Type = ViolationType.EvalCall,
                        Pattern = "eval",
                        Message = "eval() call detected - this is a critical security risk",
                        Severity = Severity.Critical
                    });
                }
                else if (id.Name == "Function")
                {
                    _violations.Add(new SecurityViolation
                    {
                        Type = ViolationType.DangerousIdentifier,
                        Pattern = "Function",
                        Message = "Function constructor detected - can execute arbitrary code",
                        Severity = Severity.Critical
                    });
                }
            }

            // Check for suspicious patterns like window.location manipulation
            if (callExpression.Callee is StaticMemberExpression member)
            {
                if (member.Object is Identifier obj && member.Property is Identifier prop)
                {
                    var pattern = $"{obj.Name}.{prop.Name}";
                    if (pattern == "window.location" || pattern == "document.write")
                    {
                        _violations.Add(new SecurityViolation
                        {
                            Type = ViolationType.SuspiciousPattern,
                            Pattern = pattern,
                            Message = $"Suspicious pattern detected: {pattern}",
                            Severity = Severity.High
                        });
                    }
                }
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override object? VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            // Check for innerHTML assignments
            if (assignmentExpression.Left is StaticMemberExpression member &&
                member.Property is Identifier prop &&
                (prop.Name == "innerHTML" || prop.Name == "outerHTML"))
            {
                _violations.Add(new SecurityViolation
                {
                    Type = ViolationType.SuspiciousPattern,
                    Pattern = prop.Name,
                    Message = $"Direct {prop.Name} assignment detected - XSS risk",
                    Severity = Severity.High
                });
            }

            return base.VisitAssignmentExpression(assignmentExpression);
        }
    }
}
