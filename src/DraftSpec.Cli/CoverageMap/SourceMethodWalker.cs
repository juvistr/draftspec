using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Roslyn syntax walker that extracts public methods from C# source files.
/// </summary>
internal sealed class SourceMethodWalker : CSharpSyntaxWalker
{
    private readonly Stack<string> _namespaceStack = new();
    private readonly Stack<string> _typeStack = new();
    private readonly List<SourceMethod> _methods = [];
    private readonly string _sourceFile;

    public SourceMethodWalker(string sourceFile)
    {
        _sourceFile = sourceFile;
    }

    /// <summary>
    /// Gets the discovered public methods.
    /// </summary>
    public IReadOnlyList<SourceMethod> Methods => _methods;

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        _namespaceStack.Push(node.Name.ToString());
        base.VisitFileScopedNamespaceDeclaration(node);
        _namespaceStack.Pop();
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        _namespaceStack.Push(node.Name.ToString());
        base.VisitNamespaceDeclaration(node);
        _namespaceStack.Pop();
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Skip non-public classes (private nested classes, etc.)
        if (!IsPubliclyAccessible(node))
        {
            return;
        }

        _typeStack.Push(node.Identifier.Text);
        base.VisitClassDeclaration(node);
        _typeStack.Pop();
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (!IsPubliclyAccessible(node))
        {
            return;
        }

        _typeStack.Push(node.Identifier.Text);
        base.VisitStructDeclaration(node);
        _typeStack.Pop();
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (!IsPubliclyAccessible(node))
        {
            return;
        }

        _typeStack.Push(node.Identifier.Text);
        base.VisitRecordDeclaration(node);
        _typeStack.Pop();
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        // Skip interfaces - we're looking for implementations
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!IsPublicMethod(node))
        {
            return;
        }

        if (_typeStack.Count == 0)
        {
            // Top-level method (C# 9+) - skip
            return;
        }

        var namespaceName = string.Join(".", _namespaceStack.Reverse());
        var className = string.Join(".", _typeStack.Reverse());
        var methodName = node.Identifier.Text;
        var signature = BuildSignature(node);
        var parameterTypes = GetParameterTypes(node);

        var fullyQualifiedName = string.IsNullOrEmpty(namespaceName)
            ? $"{className}.{methodName}"
            : $"{namespaceName}.{className}.{methodName}";

        _methods.Add(new SourceMethod
        {
            FullyQualifiedName = fullyQualifiedName,
            ClassName = _typeStack.Peek(),
            MethodName = methodName,
            Signature = signature,
            Namespace = namespaceName,
            SourceFile = _sourceFile,
            LineNumber = GetLineNumber(node),
            IsAsync = node.Modifiers.Any(SyntaxKind.AsyncKeyword),
            IsExtensionMethod = IsExtension(node),
            ParameterTypes = parameterTypes
        });
    }

    private static bool IsPubliclyAccessible(TypeDeclarationSyntax node)
    {
        // Public or internal types are accessible for testing
        return node.Modifiers.Any(m =>
            m.IsKind(SyntaxKind.PublicKeyword) ||
            m.IsKind(SyntaxKind.InternalKeyword));
    }

    private static bool IsPublicMethod(MethodDeclarationSyntax node)
    {
        // Public methods or explicitly implemented interface methods
        return node.Modifiers.Any(SyntaxKind.PublicKeyword);
    }

    private static bool IsExtension(MethodDeclarationSyntax node)
    {
        var firstParam = node.ParameterList.Parameters.FirstOrDefault();
        return firstParam?.Modifiers.Any(SyntaxKind.ThisKeyword) ?? false;
    }

    private static string BuildSignature(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;
        var typeParams = node.TypeParameterList?.ToString() ?? "";
        var parameters = node.ParameterList.Parameters
            .Select(p => GetParameterTypeName(p))
            .ToList();

        return $"{methodName}{typeParams}({string.Join(", ", parameters)})";
    }

    private static string GetParameterTypeName(ParameterSyntax parameter)
    {
        var typeName = parameter.Type?.ToString() ?? "object";

        // Simplify common generic types for readability
        return SyntaxHelpers.SimplifyTypeName(typeName);
    }

    private static List<string> GetParameterTypes(MethodDeclarationSyntax node)
    {
        return node.ParameterList.Parameters
            .Select(p => SyntaxHelpers.SimplifyTypeName(p.Type?.ToString() ?? "object"))
            .ToList();
    }

    private static int GetLineNumber(SyntaxNode node)
    {
        // Returns 1-based line number
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }
}
