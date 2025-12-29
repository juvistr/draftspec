# Implementation Guide: Line Number Filtering

## Overview

Enable running specific specs by file path and line number, critical for IDE integration and debugging workflows.

## Use Cases

1. **IDE "Run This Spec" Button**: Right-click on spec in editor → Run
2. **Debugging Specific Failures**: "Run only the spec that failed on line 42"
3. **Interactive Development**: Rapidly iterate on single spec while writing code
4. **CI Reproduction**: "Run the exact spec that failed in CI"

## Command Syntax

```bash
# Run spec at specific line
draftspec run specs/UserService.spec.csx:15

# Run multiple specs in same file
draftspec run specs/UserService.spec.csx:15,23,31

# Run specs from multiple files
draftspec run specs/UserService.spec.csx:15 specs/AuthService.spec.csx:42

# Combine with other filters (AND logic)
draftspec run specs/UserService.spec.csx:15 --tag integration

# VSCode/Rider integration - use absolute paths
draftspec run /absolute/path/specs/UserService.spec.csx:15
```

## Matching Behavior

### Exact Line Match (Strictest)

Match spec if its `LineNumber` equals one of the specified lines:

```csharp
it("creates a user", () => { });  // Line 15 - MATCHED
it("validates email", () => { }); // Line 23 - MATCHED
it("rejects duplicate", () => {}); // Line 31 - MATCHED
```

Run with: `draftspec run specs/UserService.spec.csx:15,23,31`

### Range Match (Future Enhancement)

Match specs within a line range:

```bash
# Run all specs between lines 15-50
draftspec run specs/UserService.spec.csx:15-50
```

### Context Match (Fuzzy)

If no exact line match, find the nearest spec within same context:

```csharp
describe("UserService", () => {  // Line 10
    describe("CreateAsync", () => {  // Line 11
        it("creates a user", () => { });  // Line 15
        it("validates email", () => { }); // Line 23
    });
});
```

Run with: `draftspec run specs/UserService.spec.csx:12`
→ Matches line 15 (nearest spec in CreateAsync context)

## Implementation

### 1. Extend CLI Options

**File**: `src/DraftSpec.Cli/CliOptions.cs`

```csharp
public sealed class CliOptions
{
    // Existing properties...

    /// <summary>
    /// File path and line number filters.
    /// Format: "path/to/file.spec.csx:15,23,31"
    /// </summary>
    public List<FileLineFilter> LineFilters { get; set; } = new();
}

public sealed record FileLineFilter
{
    public required string FilePath { get; init; }
    public required HashSet<int> LineNumbers { get; init; }

    /// <summary>
    /// Try parse "file.spec.csx:15,23,31" format
    /// </summary>
    public static bool TryParse(string input, out FileLineFilter? filter)
    {
        filter = null;

        var parts = input.Split(':', 2);
        if (parts.Length != 2)
            return false;

        var filePath = parts[0];
        var linesPart = parts[1];

        // Parse comma-separated line numbers
        var lineNumbers = new HashSet<int>();
        foreach (var linePart in linesPart.Split(','))
        {
            if (int.TryParse(linePart.Trim(), out var lineNum))
                lineNumbers.Add(lineNum);
            else
                return false; // Invalid line number
        }

        if (!lineNumbers.Any())
            return false;

        filter = new FileLineFilter
        {
            FilePath = filePath,
            LineNumbers = lineNumbers
        };
        return true;
    }
}
```

### 2. Parse File:Line Syntax

**File**: `src/DraftSpec.Cli/CliOptionsParser.cs`

```csharp
public static class CliOptionsParser
{
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // Check if argument is file:line format
            if (arg.Contains(':') && !arg.StartsWith("--"))
            {
                if (FileLineFilter.TryParse(arg, out var filter) && filter != null)
                {
                    options.LineFilters.Add(filter);
                    continue;
                }
            }

            // ... existing option parsing
        }

        return options;
    }
}
```

### 3. Filter Specs by Line Number

**File**: `src/DraftSpec.Cli/SpecFiltering/LineNumberFilter.cs`

```csharp
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.SpecFiltering;

public static class LineNumberFilter
{
    /// <summary>
    /// Filter specs to only those matching line number criteria.
    /// </summary>
    public static IEnumerable<DiscoveredSpec> ApplyLineFilters(
        IEnumerable<DiscoveredSpec> specs,
        List<FileLineFilter> lineFilters)
    {
        if (!lineFilters.Any())
            return specs; // No filtering

        return specs.Where(spec => MatchesAnyLineFilter(spec, lineFilters));
    }

    private static bool MatchesAnyLineFilter(DiscoveredSpec spec, List<FileLineFilter> filters)
    {
        foreach (var filter in filters)
        {
            if (MatchesLineFilter(spec, filter))
                return true;
        }
        return false;
    }

    private static bool MatchesLineFilter(DiscoveredSpec spec, FileLineFilter filter)
    {
        // Normalize paths for comparison (handle both absolute and relative)
        var specPath = NormalizePath(spec.SourceFile);
        var filterPath = NormalizePath(filter.FilePath);

        // Check if paths match (exact or relative match)
        var pathMatches = specPath.EndsWith(filterPath, StringComparison.OrdinalIgnoreCase) ||
                         filterPath.EndsWith(specPath, StringComparison.OrdinalIgnoreCase) ||
                         Path.GetFullPath(filterPath) == specPath;

        if (!pathMatches)
            return false;

        // Check if line number matches exactly
        return filter.LineNumbers.Contains(spec.LineNumber);
    }

    private static string NormalizePath(string path)
    {
        // Convert to forward slashes for consistent comparison
        return path.Replace('\\', '/');
    }
}
```

### 4. Integrate with Run Command

**File**: `src/DraftSpec.Cli/Commands/RunCommand.cs`

```csharp
public static int Execute(CliOptions options, ICliFormatterRegistry? formatterRegistry = null)
{
    // ... existing discovery code

    var discoverer = new SpecDiscoverer(projectDirectory);
    var result = await discoverer.DiscoverAsync();

    // Apply filters
    var specs = result.Specs.AsEnumerable();

    // Apply line number filters first (most specific)
    if (options.LineFilters.Any())
    {
        specs = LineNumberFilter.ApplyLineFilters(specs, options.LineFilters);

        // Show what was matched
        var matchedCount = specs.Count();
        if (matchedCount == 0)
        {
            Console.Error.WriteLine("Error: No specs found matching line number filters:");
            foreach (var filter in options.LineFilters)
            {
                var lines = string.Join(", ", filter.LineNumbers.OrderBy(x => x));
                Console.Error.WriteLine($"  {filter.FilePath}:{lines}");
            }
            return 1;
        }

        Console.WriteLine($"Matched {matchedCount} specs by line number");
    }

    // Apply other filters (name, tags, etc.)
    // ...

    // Run filtered specs
    var runner = new SpecRunner();
    var results = await runner.RunAsync(specs);

    return results.Success ? 0 : 1;
}
```

## IDE Integration Examples

### Rider Plugin (Kotlin/Java)

```kotlin
// Rider plugin action for "Run Spec at Cursor"
class RunSpecAtLineAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val file = e.getData(CommonDataKeys.VIRTUAL_FILE) ?: return
        val lineNumber = editor.caretModel.logicalPosition.line + 1

        // Build command
        val command = "draftspec run ${file.path}:$lineNumber"

        // Execute in terminal
        val project = e.project ?: return
        LocalTerminalDirectRunner(project).run(command, TerminalTabState())
    }
}
```

### VS Code Extension (TypeScript)

```typescript
// VS Code extension command
vscode.commands.registerCommand('draftspec.runSpecAtCursor', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;

    const filePath = editor.document.fileName;
    const lineNumber = editor.selection.active.line + 1;

    // Build command
    const command = `draftspec run ${filePath}:${lineNumber}`;

    // Execute in terminal
    const terminal = vscode.window.createTerminal('DraftSpec');
    terminal.show();
    terminal.sendText(command);
});
```

### JetBrains MTP Integration

```csharp
// In DraftSpec.TestingPlatform - handle run requests from IDE
public class DraftSpecTestFramework : ITestFramework
{
    public async Task RunAsync(TestExecutionRequest request, CancellationToken ct)
    {
        // IDE provides test UIDs like:
        // "specs/UserService.spec.csx:UserService/CreateAsync/creates a user"

        // Parse UID to extract file and line
        var spec = await DiscoverSpecByUid(request.TestUid);

        if (spec != null)
        {
            Console.WriteLine($"Running spec at {spec.SourceFile}:{spec.LineNumber}");
            await ExecuteSpec(spec, ct);
        }
    }
}
```

## Error Handling

### No Specs Found at Line

```bash
$ draftspec run specs/UserService.spec.csx:99

Error: No specs found matching line number filters:
  specs/UserService.spec.csx:99

Available specs in this file:
  Line 15: UserService > CreateAsync > creates a user with valid data
  Line 23: UserService > CreateAsync > validates email format
  Line 31: UserService > CreateAsync > rejects duplicate emails
  Line 45: UserService > GetAsync > returns null for missing user
```

### File Not Found

```bash
$ draftspec run specs/NonExistent.spec.csx:15

Error: File not found: specs/NonExistent.spec.csx
```

### Invalid Line Number Format

```bash
$ draftspec run specs/UserService.spec.csx:abc

Error: Invalid line number format: 'abc'
Expected format: file.spec.csx:15 or file.spec.csx:15,23,31
```

## Performance Optimization

### Early File Filtering

Only discover specs from files mentioned in line filters:

```csharp
public async Task<IReadOnlyList<DiscoveredSpec>> DiscoverFromFilesAsync(
    IEnumerable<string> filePaths,
    CancellationToken ct = default)
{
    var allSpecs = new List<DiscoveredSpec>();

    foreach (var filePath in filePaths)
    {
        var specs = await DiscoverFileAsync(filePath, ct);
        allSpecs.AddRange(specs);
    }

    return allSpecs;
}
```

Usage:
```csharp
// Only discover specified files, not entire project
var filesToDiscover = options.LineFilters.Select(f => f.FilePath).Distinct();
var specs = await discoverer.DiscoverFromFilesAsync(filesToDiscover);

// Then apply line number filtering
var filteredSpecs = LineNumberFilter.ApplyLineFilters(specs, options.LineFilters);
```

### Caching for Watch Mode

Cache line-to-spec mappings for instant lookup:

```csharp
public class LineNumberCache
{
    private readonly Dictionary<string, SortedList<int, DiscoveredSpec>> _cache = new();

    public void Index(IEnumerable<DiscoveredSpec> specs)
    {
        foreach (var spec in specs)
        {
            if (!_cache.TryGetValue(spec.SourceFile, out var lineMap))
            {
                lineMap = new SortedList<int, DiscoveredSpec>();
                _cache[spec.SourceFile] = lineMap;
            }

            lineMap[spec.LineNumber] = spec;
        }
    }

    public DiscoveredSpec? FindByLine(string filePath, int lineNumber)
    {
        if (!_cache.TryGetValue(filePath, out var lineMap))
            return null;

        lineMap.TryGetValue(lineNumber, out var spec);
        return spec;
    }

    public IEnumerable<DiscoveredSpec> FindByLines(string filePath, IEnumerable<int> lineNumbers)
    {
        if (!_cache.TryGetValue(filePath, out var lineMap))
            yield break;

        foreach (var lineNum in lineNumbers)
        {
            if (lineMap.TryGetValue(lineNum, out var spec))
                yield return spec;
        }
    }
}
```

## Testing

### Unit Tests

**File**: `tests/DraftSpec.Cli.Tests/SpecFiltering/LineNumberFilterTests.cs`

```csharp
public class LineNumberFilterTests
{
    [Fact]
    public void ApplyLineFilters_MatchesExactLine()
    {
        // Arrange
        var specs = new[]
        {
            CreateSpec("specs/test.spec.csx", 15, "spec 1"),
            CreateSpec("specs/test.spec.csx", 23, "spec 2"),
            CreateSpec("specs/test.spec.csx", 31, "spec 3")
        };

        var filters = new List<FileLineFilter>
        {
            new() { FilePath = "specs/test.spec.csx", LineNumbers = new HashSet<int> { 23 } }
        };

        // Act
        var result = LineNumberFilter.ApplyLineFilters(specs, filters).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Description.Should().Be("spec 2");
        result[0].LineNumber.Should().Be(23);
    }

    [Fact]
    public void ApplyLineFilters_MatchesMultipleLines()
    {
        var specs = new[]
        {
            CreateSpec("specs/test.spec.csx", 15, "spec 1"),
            CreateSpec("specs/test.spec.csx", 23, "spec 2"),
            CreateSpec("specs/test.spec.csx", 31, "spec 3")
        };

        var filters = new List<FileLineFilter>
        {
            new() { FilePath = "specs/test.spec.csx", LineNumbers = new HashSet<int> { 15, 31 } }
        };

        var result = LineNumberFilter.ApplyLineFilters(specs, filters).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(s => s.LineNumber == 15);
        result.Should().Contain(s => s.LineNumber == 31);
    }

    [Fact]
    public void ApplyLineFilters_HandlesRelativeAndAbsolutePaths()
    {
        var specs = new[]
        {
            CreateSpec("/absolute/path/specs/test.spec.csx", 15, "spec 1")
        };

        var filters = new List<FileLineFilter>
        {
            new() { FilePath = "specs/test.spec.csx", LineNumbers = new HashSet<int> { 15 } }
        };

        var result = LineNumberFilter.ApplyLineFilters(specs, filters).ToList();

        result.Should().HaveCount(1);
    }

    private static DiscoveredSpec CreateSpec(string filePath, int lineNumber, string description)
    {
        return new DiscoveredSpec
        {
            Id = $"{filePath}:{lineNumber}",
            Description = description,
            DisplayName = description,
            ContextPath = Array.Empty<string>(),
            SourceFile = filePath,
            RelativeSourceFile = filePath,
            LineNumber = lineNumber
        };
    }
}
```

### Integration Tests

```bash
# Create test spec file
cat > /tmp/test.spec.csx << 'EOF'
using static DraftSpec.Dsl;

describe("Test", () => {
    it("spec at line 4", () => {});  // Line 4
    it("spec at line 5", () => {});  // Line 5
    it("spec at line 6", () => {});  // Line 6
});

run();
EOF

# Test exact line match
draftspec run /tmp/test.spec.csx:5
# Expected: Runs only "spec at line 5"

# Test multiple lines
draftspec run /tmp/test.spec.csx:4,6
# Expected: Runs "spec at line 4" and "spec at line 6"

# Test invalid line
draftspec run /tmp/test.spec.csx:99
# Expected: Error - no specs found
```

## Future Enhancements

### 1. Nearest Spec Matching

If exact line doesn't match a spec, find nearest:

```bash
$ draftspec run specs/UserService.spec.csx:20

Info: No spec at line 20, running nearest spec at line 23:
  UserService > CreateAsync > validates email format
```

### 2. Range Syntax

```bash
# Run all specs in line range
draftspec run specs/UserService.spec.csx:15-50
```

### 3. Column Support (for inline specs)

```bash
# Future: support multiple specs on same line
draftspec run specs/test.spec.csx:15:42  # line 15, column 42
```

### 4. Diff-Based Filtering

```bash
# Run only specs in changed lines (for watch mode)
git diff HEAD --unified=0 | draftspec run --changed-lines
```

## Documentation

Add to `README.md`:

```markdown
### Run Specific Specs by Line Number

Run individual specs using file path and line number:

```bash
# Run spec at line 15
draftspec run specs/UserService.spec.csx:15

# Run multiple specs
draftspec run specs/UserService.spec.csx:15,23,31

# IDE integration - right-click "Run this spec"
```

Perfect for:
- IDE integration (run/debug buttons)
- Debugging specific failures
- Rapid iteration during development
```

## Summary

Line number filtering enables:
- **IDE Integration**: Native "Run this spec" buttons
- **Precise Debugging**: Target exact failing spec
- **Fast Iteration**: Skip full test suite during development
- **CI Reproduction**: Re-run specific failing specs

Implementation leverages existing static parsing infrastructure with minimal new code (~200 LOC).
