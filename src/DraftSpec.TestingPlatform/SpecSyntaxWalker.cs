using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Roslyn syntax walker that discovers spec definitions by analyzing syntax trees.
/// </summary>
/// <remarks>
/// This walker finds describe/context/it/fit/xit method calls without executing the code,
/// allowing spec discovery even when files have compilation errors.
/// </remarks>
internal sealed class SpecSyntaxWalker : CSharpSyntaxWalker
{
    private readonly Stack<string> _contextStack = new();
    private readonly List<StaticSpec> _specs = [];
    private readonly List<string> _warnings = [];
    private bool _isComplete = true;

    /// <summary>
    /// Gets the discovered specs.
    /// </summary>
    public IReadOnlyList<StaticSpec> Specs => _specs;

    /// <summary>
    /// Gets warnings about patterns that couldn't be fully analyzed.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// Gets whether all spec patterns were successfully analyzed.
    /// </summary>
    public bool IsComplete => _isComplete;

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var methodName = GetMethodName(node);

        if (methodName is "describe" or "context" or "fdescribe" or "xdescribe")
        {
            ProcessDescribeBlock(node, methodName);
            return; // Don't auto-descend - we manually visit the lambda
        }

        if (methodName is "it" or "fit" or "xit")
        {
            ProcessSpecDefinition(node, methodName);
            return; // Don't descend into spec body
        }

        // Continue normal traversal for other invocations
        base.VisitInvocationExpression(node);
    }

    private void ProcessDescribeBlock(InvocationExpressionSyntax node, string methodName)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count < 1)
        {
            AddWarning(node, $"'{methodName}' call missing description argument");
            return;
        }

        // Extract description from first argument
        var description = ExtractStringLiteral(arguments[0].Expression);
        if (description == null)
        {
            AddWarning(node, $"'{methodName}' has dynamic description - cannot analyze statically");
            _isComplete = false;
            // Still try to visit children in case there are nested specs we can find
            description = $"<dynamic at line {GetLineNumber(node)}>";
        }

        _contextStack.Push(description);

        // Find and visit the lambda body (second argument)
        if (arguments.Count >= 2)
        {
            var lambdaArg = arguments[1].Expression;
            VisitLambdaBody(lambdaArg);
        }

        _contextStack.Pop();
    }

    private void ProcessSpecDefinition(InvocationExpressionSyntax node, string methodName)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count < 1)
        {
            AddWarning(node, $"'{methodName}' call missing description argument");
            return;
        }

        // Extract description from first argument
        var description = ExtractStringLiteral(arguments[0].Expression);
        if (description == null)
        {
            AddWarning(node, $"'{methodName}' has dynamic description - cannot analyze statically");
            _isComplete = false;
            return; // Skip specs with dynamic descriptions
        }

        var specType = methodName switch
        {
            "fit" => StaticSpecType.Focused,
            "xit" => StaticSpecType.Skipped,
            _ => StaticSpecType.Regular
        };

        // Check if pending (no body argument)
        var isPending = arguments.Count == 1;

        _specs.Add(new StaticSpec
        {
            Description = description,
            ContextPath = _contextStack.Reverse().ToList(),
            LineNumber = GetLineNumber(node),
            Type = specType,
            IsPending = isPending
        });
    }

    private void VisitLambdaBody(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case ParenthesizedLambdaExpressionSyntax lambda:
                if (lambda.Body != null)
                {
                    Visit(lambda.Body);
                }
                break;

            case SimpleLambdaExpressionSyntax simpleLambda:
                if (simpleLambda.Body != null)
                {
                    Visit(simpleLambda.Body);
                }
                break;

            case AnonymousMethodExpressionSyntax anonymousMethod:
                if (anonymousMethod.Body != null)
                {
                    Visit(anonymousMethod.Body);
                }
                break;

            case IdentifierNameSyntax:
                // Method group - can't analyze
                _isComplete = false;
                break;
        }
    }

    private static string? ExtractStringLiteral(ExpressionSyntax expression)
    {
        return expression switch
        {
            // Simple string literal: "description"
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal
                => literal.Token.ValueText,

            // Verbatim string: @"description"
            LiteralExpressionSyntax { Token.RawKind: (int)SyntaxKind.StringLiteralToken } verbatim
                => verbatim.Token.ValueText,

            // Parenthesized: ("description")
            ParenthesizedExpressionSyntax paren
                => ExtractStringLiteral(paren.Expression),

            // For interpolated strings, const expressions, etc. - return null
            _ => null
        };
    }

    private static string? GetMethodName(InvocationExpressionSyntax node)
    {
        return node.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }

    private static int GetLineNumber(SyntaxNode node)
    {
        // Returns 1-based line number
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }

    private void AddWarning(SyntaxNode node, string message)
    {
        var lineNumber = GetLineNumber(node);
        _warnings.Add($"Line {lineNumber}: {message}");
    }
}
