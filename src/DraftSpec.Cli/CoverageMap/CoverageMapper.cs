namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Maps specs to source methods and computes coverage confidence.
/// </summary>
public sealed class CoverageMapper
{
    /// <summary>
    /// Maps source methods to specs and computes coverage.
    /// </summary>
    public CoverageMapResult Map(
        IReadOnlyList<SourceMethod> methods,
        IReadOnlyList<SpecReference> specReferences,
        string? sourcePath = null,
        string? specPath = null)
    {
        var methodCoverages = new List<MethodCoverage>();

        foreach (var method in methods)
        {
            var coveringSpecs = new List<SpecCoverageInfo>();
            var highestConfidence = CoverageConfidence.None;

            foreach (var spec in specReferences)
            {
                var (confidence, reason) = ComputeConfidence(method, spec);

                if (confidence != CoverageConfidence.None)
                {
                    coveringSpecs.Add(new SpecCoverageInfo
                    {
                        SpecId = spec.SpecId,
                        DisplayName = BuildDisplayName(spec),
                        Confidence = confidence,
                        MatchReason = reason,
                        SpecFile = GetRelativeSpecFile(spec),
                        LineNumber = spec.LineNumber
                    });

                    if (confidence > highestConfidence)
                    {
                        highestConfidence = confidence;
                    }
                }
            }

            methodCoverages.Add(new MethodCoverage
            {
                Method = method,
                Confidence = highestConfidence,
                CoveringSpecs = coveringSpecs
                    .OrderByDescending(s => s.Confidence)
                    .ThenBy(s => s.DisplayName)
                    .ToList()
            });
        }

        return new CoverageMapResult
        {
            AllMethods = methodCoverages,
            Summary = ComputeSummary(methodCoverages),
            SourcePath = sourcePath,
            SpecPath = specPath
        };
    }

    private static (CoverageConfidence confidence, string? reason) ComputeConfidence(
        SourceMethod method,
        SpecReference spec)
    {
        // HIGH: Direct method call in spec body
        foreach (var call in spec.MethodCalls)
        {
            if (MethodNameMatches(method, call))
            {
                return (CoverageConfidence.High, $"Direct call: {call.MethodName}()");
            }
        }

        // MEDIUM: Type reference in spec body (new, cast, typeof, etc.)
        foreach (var typeRef in spec.TypeReferences)
        {
            if (TypeMatches(method.ClassName, typeRef.TypeName))
            {
                var kindDescription = typeRef.Kind switch
                {
                    ReferenceKind.New => "new",
                    ReferenceKind.Cast => "cast to",
                    ReferenceKind.TypeOf => "typeof",
                    ReferenceKind.Variable => "variable of type",
                    _ => "reference to"
                };
                return (CoverageConfidence.Medium, $"Type reference: {kindDescription} {typeRef.TypeName}");
            }
        }

        // LOW: Namespace match only (via using directives)
        foreach (var ns in spec.UsingNamespaces)
        {
            if (NamespaceMatches(method.Namespace, ns))
            {
                return (CoverageConfidence.Low, $"Namespace match: using {ns}");
            }
        }

        return (CoverageConfidence.None, null);
    }

    private static bool MethodNameMatches(SourceMethod method, MethodCall call)
    {
        // Exact name match
        if (method.MethodName.Equals(call.MethodName, StringComparison.Ordinal))
        {
            return true;
        }

        // Handle async variants: CreateAsync matches Create
        if (method.IsAsync && method.MethodName.EndsWith("Async", StringComparison.Ordinal))
        {
            var syncName = method.MethodName[..^5];
            if (syncName.Equals(call.MethodName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        // Handle non-async calling async: Create matches CreateAsync
        if (call.MethodName.EndsWith("Async", StringComparison.Ordinal))
        {
            var syncCallName = call.MethodName[..^5];
            if (method.MethodName.Equals(syncCallName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TypeMatches(string className, string typeRef)
    {
        // Exact match
        if (className.Equals(typeRef, StringComparison.Ordinal))
        {
            return true;
        }

        // Handle generic type names: List matches List<T>
        if (typeRef.Contains('<'))
        {
            var genericBase = typeRef.Split('<')[0];
            if (className.Equals(genericBase, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NamespaceMatches(string methodNamespace, string usingNamespace)
    {
        if (string.IsNullOrEmpty(methodNamespace) || string.IsNullOrEmpty(usingNamespace))
        {
            return false;
        }

        // Direct match
        if (methodNamespace.Equals(usingNamespace, StringComparison.Ordinal))
        {
            return true;
        }

        // Method namespace is a child of the using namespace
        // e.g., method in "TodoApi.Services.Internal" matches "using TodoApi.Services"
        if (methodNamespace.StartsWith(usingNamespace + ".", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string BuildDisplayName(SpecReference spec)
    {
        if (spec.ContextPath.Count == 0)
        {
            return spec.SpecDescription;
        }

        return string.Join(" > ", spec.ContextPath) + " > " + spec.SpecDescription;
    }

    private static string? GetRelativeSpecFile(SpecReference spec)
    {
        // Extract just the file name or relative path from the spec ID
        var colonIndex = spec.SpecId.IndexOf(':');
        return colonIndex >= 0 ? spec.SpecId[..colonIndex] : null;
    }

    private static CoverageSummary ComputeSummary(IReadOnlyList<MethodCoverage> methodCoverages)
    {
        var total = methodCoverages.Count;
        var high = methodCoverages.Count(m => m.Confidence == CoverageConfidence.High);
        var medium = methodCoverages.Count(m => m.Confidence == CoverageConfidence.Medium);
        var low = methodCoverages.Count(m => m.Confidence == CoverageConfidence.Low);
        var none = methodCoverages.Count(m => m.Confidence == CoverageConfidence.None);

        return new CoverageSummary
        {
            TotalMethods = total,
            HighConfidence = high,
            MediumConfidence = medium,
            LowConfidence = low,
            Uncovered = none
        };
    }
}
