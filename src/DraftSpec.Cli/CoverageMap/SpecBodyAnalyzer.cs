using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Roslyn syntax walker that extracts method calls and type references from spec bodies.
/// </summary>
internal sealed class SpecBodyAnalyzer : CSharpSyntaxWalker
{
    private readonly Stack<string> _contextStack = new();
    private readonly List<SpecReference> _specReferences = [];
    private readonly HashSet<string> _usingNamespaces = new(StringComparer.Ordinal);
    private readonly string _sourceFile;
    private readonly string _projectPath;

    // Current spec being analyzed
    private string? _currentSpecDescription;
    private int _currentSpecLineNumber;
    private List<MethodCall>? _currentMethodCalls;
    private List<TypeReference>? _currentTypeReferences;
    private bool _insideSpecBody;

    public SpecBodyAnalyzer(string sourceFile, string projectPath)
    {
        _sourceFile = sourceFile;
        _projectPath = projectPath;
    }

    /// <summary>
    /// Gets the analyzed spec references with their method/type references.
    /// </summary>
    public IReadOnlyList<SpecReference> SpecReferences => _specReferences;

    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var namespaceName = node.Name?.ToString();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            _usingNamespaces.Add(namespaceName);
        }
        base.VisitUsingDirective(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var methodName = SyntaxHelpers.GetMethodName(node);

        if (methodName is "describe" or "context" or "fdescribe" or "xdescribe")
        {
            ProcessDescribeBlock(node);
            return;
        }

        if (methodName is "it" or "fit")
        {
            ProcessSpecWithBody(node);
            return;
        }

        // Skip hooks - we only care about spec bodies
        if (methodName is "before" or "after" or "beforeAll" or "afterAll" or "beforeEach" or "afterEach")
        {
            return;
        }

        // If inside a spec body, record this method call
        if (_insideSpecBody && methodName != null)
        {
            RecordMethodCall(node, methodName);
        }

        base.VisitInvocationExpression(node);
    }

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (_insideSpecBody)
        {
            var typeName = SyntaxHelpers.GetSimpleTypeName(node.Type);
            if (typeName != null)
            {
                _currentTypeReferences?.Add(new TypeReference
                {
                    TypeName = typeName,
                    Kind = ReferenceKind.New,
                    LineNumber = SyntaxHelpers.GetLineNumber(node)
                });
            }
        }
        base.VisitObjectCreationExpression(node);
    }

    public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
    {
        // new() syntax - type is inferred, we can't extract it without semantic analysis
        base.VisitImplicitObjectCreationExpression(node);
    }

    public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
    {
        if (_insideSpecBody)
        {
            var typeName = SyntaxHelpers.GetSimpleTypeName(node.Type);
            if (typeName != null)
            {
                _currentTypeReferences?.Add(new TypeReference
                {
                    TypeName = typeName,
                    Kind = ReferenceKind.TypeOf,
                    LineNumber = SyntaxHelpers.GetLineNumber(node)
                });
            }
        }
        base.VisitTypeOfExpression(node);
    }

    public override void VisitCastExpression(CastExpressionSyntax node)
    {
        if (_insideSpecBody)
        {
            var typeName = SyntaxHelpers.GetSimpleTypeName(node.Type);
            if (typeName != null)
            {
                _currentTypeReferences?.Add(new TypeReference
                {
                    TypeName = typeName,
                    Kind = ReferenceKind.Cast,
                    LineNumber = SyntaxHelpers.GetLineNumber(node)
                });
            }
        }
        base.VisitCastExpression(node);
    }

    public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
    {
        if (_insideSpecBody)
        {
            var typeName = SyntaxHelpers.GetSimpleTypeName(node.Type);
            // Skip var and implicit types
            if (typeName != null && !string.Equals(typeName, "var", StringComparison.Ordinal))
            {
                _currentTypeReferences?.Add(new TypeReference
                {
                    TypeName = typeName,
                    Kind = ReferenceKind.Variable,
                    LineNumber = SyntaxHelpers.GetLineNumber(node)
                });
            }
        }
        base.VisitVariableDeclaration(node);
    }

    private void ProcessDescribeBlock(InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count < 1)
        {
            return;
        }

        var description = SyntaxHelpers.ExtractStringLiteral(arguments[0].Expression);
        if (description == null)
        {
            description = $"<dynamic at line {SyntaxHelpers.GetLineNumber(node)}>";
        }

        _contextStack.Push(description);

        // Visit the lambda body
        if (arguments.Count >= 2)
        {
            VisitLambdaBody(arguments[1].Expression);
        }

        _contextStack.Pop();
    }

    private void ProcessSpecWithBody(InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count < 2)
        {
            // Pending spec (no body) - skip
            return;
        }

        var description = SyntaxHelpers.ExtractStringLiteral(arguments[0].Expression);
        if (description == null)
        {
            return;
        }

        // Set up collection for this spec
        _currentSpecDescription = description;
        _currentSpecLineNumber = SyntaxHelpers.GetLineNumber(node);
        _currentMethodCalls = [];
        _currentTypeReferences = [];
        _insideSpecBody = true;

        // Visit the spec body
        VisitLambdaBody(arguments[1].Expression);

        // Create the spec reference
        var relativePath = Path.GetRelativePath(_projectPath, _sourceFile);
        var contextPath = _contextStack.Reverse().ToList();
        var contextPathString = string.Join("/", contextPath);
        var specId = $"{relativePath.Replace('\\', '/')}:{contextPathString}/{description}";

        _specReferences.Add(new SpecReference
        {
            SpecId = specId,
            SpecDescription = description,
            ContextPath = contextPath,
            MethodCalls = _currentMethodCalls,
            TypeReferences = _currentTypeReferences,
            UsingNamespaces = _usingNamespaces.ToList(),
            SourceFile = _sourceFile,
            LineNumber = _currentSpecLineNumber
        });

        // Reset state
        _insideSpecBody = false;
        _currentMethodCalls = null;
        _currentTypeReferences = null;
        _currentSpecDescription = null;
    }

    private void RecordMethodCall(InvocationExpressionSyntax node, string methodName)
    {
        string? receiverName = null;

        // Try to get the receiver (e.g., "service" in "service.CreateAsync()")
        if (node.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax id })
        {
            receiverName = id.Identifier.Text;
        }

        _currentMethodCalls?.Add(new MethodCall
        {
            MethodName = methodName,
            ReceiverName = receiverName,
            LineNumber = SyntaxHelpers.GetLineNumber(node)
        });
    }

    private void VisitLambdaBody(ExpressionSyntax expression)
    {
        // Specs always use parenthesized lambda syntax: () => { }
        // SimpleLambda (x => ...) and AnonymousMethod (delegate { }) are not used by the DSL
        if (expression is ParenthesizedLambdaExpressionSyntax lambda && lambda.Body != null)
        {
            Visit(lambda.Body);
        }
    }
}
