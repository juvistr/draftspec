# Security Audit

## Summary

DraftSpec executes user-provided C# test code via CSX scripts. The audit identified **3 High severity**, **4 Medium severity**, and **3 Low severity** issues. Primary concerns: arbitrary code execution, command injection, and path traversal.

**Overall Risk:** HIGH (due to executing user-provided code)

## High Severity Issues

### H-1: Command Injection via Unvalidated File Paths

**Location:** `src/DraftSpec.Cli/ProcessHelper.cs:15-34`
**OWASP:** A03:2021 - Injection

**Problem:** Arguments passed directly to process without validation:
```csharp
var psi = new ProcessStartInfo
{
    FileName = fileName,
    Arguments = arguments,  // Unvalidated user input
};
```

**Attack Scenario:**
```bash
dotnet script "file.spec.csx && malicious-command"
```

**Remediation:** Use `ArgumentList` instead of `Arguments`:
```csharp
var psi = new ProcessStartInfo
{
    FileName = fileName,
    UseShellExecute = false,
};
// Parse and add arguments individually
foreach (var arg in ParseArguments(arguments))
{
    psi.ArgumentList.Add(arg);
}
```

### H-2: Path Traversal in Spec File Discovery

**Location:** `src/DraftSpec.Cli/SpecFinder.cs:5-29`
**OWASP:** A01:2021 - Broken Access Control

**Problem:** Accepts arbitrary paths without validating they don't escape project:
```csharp
public IReadOnlyList<string> FindSpecs(string path)
{
    var fullPath = Path.GetFullPath(path);  // Resolves but doesn't validate
    // ...
}
```

**Attack Scenario:**
```bash
draftspec run ../../etc/passwd.spec.csx
draftspec run ../../../sensitive/data.spec.csx
```

**Remediation:**
```csharp
public IReadOnlyList<string> FindSpecs(string path)
{
    var fullPath = Path.GetFullPath(path);
    var currentDir = Directory.GetCurrentDirectory();

    if (!fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase))
    {
        throw new SecurityException($"Path traversal detected: {path}");
    }
    // ...
}
```

### H-3: Insecure Temporary File Handling

**Location:** `src/DraftSpec.Cli/SpecFileRunner.cs:130-167`
**OWASP:** A01:2021 - Broken Access Control

**Problem:** Predictable temp file names based on spec file:
```csharp
var tempFile = Path.Combine(workingDir,
    $".{Path.GetFileNameWithoutExtension(fileName)}.json.csx");
File.WriteAllText(tempFile, modifiedScript);
```

**Issues:**
- TOCTOU (time-of-check-time-of-use) race condition
- Attacker could create symlink at temp file location
- Temp files may leak test data

**Remediation:**
```csharp
var tempFile = Path.Combine(
    Path.GetTempPath(),
    $"{Guid.NewGuid():N}.spec.csx");
```

## Medium Severity Issues

### M-1: Unsafe String Replacement

**Location:** `src/DraftSpec.Cli/SpecFileRunner.cs:138-140`
**OWASP:** A03:2021 - Injection

**Problem:** Naive replacement can be bypassed:
```csharp
.Replace("run();", "run(json: true);")
.Replace("run()", "run(json: true)");
```

Could match in comments/strings, bypass with `run(/*comment*/)`.

**Remediation:** Use proper C# parsing with Roslyn or regex with word boundaries.

### M-2: Missing Input Validation

**Locations:**
- `NewCommand.cs:33-34` - Spec name not validated for path traversal
- `InitCommand.cs:42,57` - Overwrites files without backup
- `RunCommand.cs:94` - Output file path not validated

**Remediation:**
- Validate inputs against allowlist of safe characters
- Reject paths containing `..`, `/`, `\`
- Create backups before overwriting

### M-3: XSS in HTML Formatter

**Location:** `src/DraftSpec.Formatters.Html/HtmlFormatter.cs:31-32, 42-44`
**OWASP:** A03:2021 - Injection (XSS)

**Problem:** CustomCss not escaped:
```csharp
if (!string.IsNullOrEmpty(_options.CustomCss))
{
    sb.AppendLine(_options.CustomCss);  // NOT ESCAPED!
}
```

**Attack:**
```csharp
new HtmlOptions {
    CustomCss = "</style><script>alert('XSS')</script><style>"
}
```

**Remediation:** Sanitize CSS content or use CSP-compliant sanitizer.

### M-4: Insecure Temp File Cleanup

**Location:** `src/DraftSpec.Cli/SpecFileRunner.cs:159-166`

**Problem:** Cleanup in finally block doesn't handle access denied:
```csharp
finally
{
    if (File.Exists(tempFile))
        File.Delete(tempFile);  // Can throw
}
```

**Remediation:**
```csharp
finally
{
    try
    {
        if (File.Exists(tempFile))
        {
            File.SetAttributes(tempFile, FileAttributes.Normal);
            File.Delete(tempFile);
        }
    }
    catch (Exception ex)
    {
        // Log but don't throw
    }
}
```

## Low Severity Issues

### L-1: Information Disclosure in Error Messages

**Problem:** Assertion errors expose data via ToString() without sanitization.

### L-2: No Rate Limiting

**Problem:** Parallel execution has no limits beyond ProcessorCount.

### L-3: Missing Security Headers in HTML

**Problem:** Generated HTML lacks CSP, X-Content-Type-Options, X-Frame-Options.

## Informational

- No dependency vulnerability scanning
- Missing SBOM (Software Bill of Materials)
- No SECURITY.md with reporting process

## Positive Security Practices

- Nullable reference types enabled
- No SQL/database usage
- Minimal dependencies
- HTML encoding used in most places
- UseShellExecute = false
- Safe JSON serialization defaults

## OWASP Top 10 Assessment

| Category | Risk | Findings |
|----------|------|----------|
| A01: Broken Access Control | HIGH | Path traversal (H-2), File write (H-3) |
| A02: Cryptographic Failures | LOW | N/A |
| A03: Injection | HIGH | Command injection (H-1), XSS (M-3) |
| A04: Insecure Design | MEDIUM | No rate limiting |
| A05: Security Misconfiguration | LOW | Missing headers |
| A06: Vulnerable Components | INFO | No scanning |
| A07: Auth | N/A | Not applicable |
| A08: Software Integrity | MEDIUM | No SBOM |
| A09: Logging Failures | INFO | Minimal logging |
| A10: SSRF | N/A | No external requests |

## CWE Mappings

- CWE-22: Path Traversal (H-2)
- CWE-78: OS Command Injection (H-1)
- CWE-79: Cross-site Scripting (M-3)
- CWE-377: Insecure Temporary File (H-3)
- CWE-73: External Control of File Name (M-2)

## Remediation Priority

### Immediate (1-2 weeks)
1. Fix path traversal (H-2)
2. Secure temp file handling (H-3)
3. Add input validation (M-2)

### Short-term (1 month)
4. Fix command injection (H-1)
5. Sanitize CustomCss (M-3)
6. Add SECURITY.md

### Medium-term
7. Rate limiting and timeouts
8. Dependency scanning in CI
9. Security headers in HTML

## Conclusion

**Do not execute untrusted spec files without sandboxing.**

The framework needs security hardening before production use, particularly around path validation and process execution.
