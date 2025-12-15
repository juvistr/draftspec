# ADR-006: Security Hardening for File Operations

**Status:** Implemented ✅
**Date:** 2025-12-15
**Deciders:** DraftSpec maintainers

## Context

A pre-v1.0 security review identified vulnerabilities in file operations that could be exploited in CI/CD environments or when processing untrusted spec files.

**Key findings:**

1. **Temp file race condition (HIGH)** - `SpecFileRunner.cs:131-178`
   - `RunWithJson` creates temp files with predictable paths
   - Attacker could win race to create symlink, causing arbitrary file write

2. **Path traversal bypass (HIGH)** - `SpecFinder.cs:20-24`
   - `StartsWith` check without trailing separator
   - `/allowed/path-evil/` bypasses `/allowed/path` validation

3. **JSON deserialization DoS (MEDIUM)** - `SpecReport.cs:19-27`
   - No depth or size limits on JSON parsing
   - Malicious spec output could cause stack overflow or memory exhaustion

**Good practices already present:**

- `ArgumentList` for process arguments (prevents command injection)
- `HttpUtility.HtmlEncode` in HTML formatter (prevents XSS)
- `UseShellExecute = false` (prevents shell injection)
- Nullable reference types enabled

## Decision

Implement the following security hardening measures:

### 1. Secure Temp File Creation

**Before:**
```csharp
var tempFile = Path.Combine(Path.GetTempPath(), $"draftspec_{Guid.NewGuid()}.json");
File.WriteAllText(tempFile, json);
```

**After:**
```csharp
var tempFile = Path.Combine(Path.GetTempPath(), $"draftspec_{Guid.NewGuid()}.json");
using var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write,
    FileShare.None, 4096, FileOptions.DeleteOnClose);
using var writer = new StreamWriter(fs);
await writer.WriteAsync(json);
```

- `FileMode.CreateNew` fails if file exists (prevents race)
- `FileOptions.DeleteOnClose` ensures cleanup
- `FileShare.None` prevents concurrent access

### 2. Path Traversal Prevention

**Before:**
```csharp
if (!fullPath.StartsWith(basePath))
    throw new InvalidOperationException("Path outside base directory");
```

**After:**
```csharp
var normalizedBase = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar)
    + Path.DirectorySeparatorChar;
var normalizedPath = Path.GetFullPath(fullPath);

var comparison = OperatingSystem.IsWindows()
    ? StringComparison.OrdinalIgnoreCase
    : StringComparison.Ordinal;

if (!normalizedPath.StartsWith(normalizedBase, comparison))
    throw new InvalidOperationException("Path outside base directory");
```

- Trailing separator prevents prefix attacks
- Platform-appropriate case sensitivity

### 3. JSON Deserialization Limits

**Before:**
```csharp
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
return JsonSerializer.Deserialize<SpecReport>(json, options);
```

**After:**
```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    MaxDepth = 64
};

if (json.Length > 10_000_000) // 10MB limit
    throw new InvalidOperationException("Report too large");

return JsonSerializer.Deserialize<SpecReport>(json, options);
```

- `MaxDepth = 64` prevents stack overflow from deeply nested JSON
- Size limit prevents memory exhaustion

## Consequences

### Positive

- **CI/CD safe** - Can run untrusted specs without file system risk
- **Defense in depth** - Multiple layers of validation
- **Standard patterns** - Uses well-known secure coding practices
- **Minimal overhead** - Security checks add negligible latency

### Negative

- **Complexity** - More code to maintain
- **Potential breaks** - Edge cases with path handling may surface
- **Limits** - JSON size/depth limits could reject valid large reports

### Neutral

- **Testing** - Each fix needs corresponding security tests
- **Documentation** - Users should understand any new limits

## Implementation Notes

**Files to modify:**

1. `src/DraftSpec.Cli/SpecFileRunner.cs` - Temp file handling
2. `src/DraftSpec.Cli/SpecFinder.cs` - Path validation
3. `src/DraftSpec.Formatters.Abstractions/SpecReport.cs` - JSON limits
4. `src/DraftSpec/Plugins/Reporters/FileReporter.cs` - Output path validation

**Testing approach:**

- Unit tests for each vulnerability scenario
- Integration test with symlink attack (on supported platforms)
- Fuzzing for path traversal edge cases

## References

- [CWE-367: Time-of-check Time-of-use (TOCTOU) Race Condition](https://cwe.mitre.org/data/definitions/367.html)
- [CWE-22: Path Traversal](https://cwe.mitre.org/data/definitions/22.html)
- [CWE-400: Uncontrolled Resource Consumption](https://cwe.mitre.org/data/definitions/400.html)
