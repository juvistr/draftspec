using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Static helper methods for Roslyn syntax analysis.
/// Extracted for independent testability.
/// </summary>
internal static class SyntaxHelpers
{
    /// <summary>
    /// Extracts the string value from a literal expression.
    /// Handles regular strings, verbatim strings, raw strings, and parenthesized expressions.
    /// </summary>
    public static string? ExtractStringLiteral(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal
                => literal.Token.ValueText,
            ParenthesizedExpressionSyntax paren
                => ExtractStringLiteral(paren.Expression),
            _ => null
        };
    }

    /// <summary>
    /// Gets the method name from an invocation expression.
    /// </summary>
    public static string? GetMethodName(InvocationExpressionSyntax node)
    {
        return node.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    /// Gets the simple (unqualified) name of a type.
    /// </summary>
    public static string? GetSimpleTypeName(TypeSyntax type)
    {
        return type switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            NullableTypeSyntax nullable => GetSimpleTypeName(nullable.ElementType),
            ArrayTypeSyntax array => GetSimpleTypeName(array.ElementType),
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,
            _ => null
        };
    }

    /// <summary>
    /// Gets the 1-based line number for a syntax node.
    /// </summary>
    public static int GetLineNumber(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }
}
