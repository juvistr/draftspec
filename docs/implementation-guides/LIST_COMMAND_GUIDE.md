# Implementation Guide: `draftspec list` Command

## Overview

The `list` command discovers and displays all specs without executing them, using the static parsing capability from v0.4.0.

## User Stories

1. **CI Pipeline Introspection**: "What specs will run in this build?"
2. **Documentation Generation**: "Export all specs as a checklist for stakeholders"
3. **Filter Preview**: "What specs match my filter before I run them?"
4. **IDE Integration**: "Show outline of all specs in project"

## Command Syntax

```bash
# Basic usage
draftspec list [path]

# Options
draftspec list . --show-line-numbers
draftspec list . --focused-only
draftspec list . --pending-only
draftspec list . --skipped-only
draftspec list . --filter "UserService"
draftspec list . --context "Authentication/*"
draftspec list . --format [tree|flat|json|csv]
draftspec list . --output specs.json
draftspec list . --show-compilation-errors
```

## Output Formats

### Tree Format (Default)

```
specs/UserService.spec.csx
  UserService
    ├─ CreateAsync
    │  ├─ ✓ it creates a user with valid data (line 15)
    │  ├─ ✓ it validates email format (line 23)
    │  └─ ✓ it rejects duplicate emails (line 31)
    ├─ GetAsync
    │  ├─ ✓ it returns null for missing user (line 45)
    │  └─ ✓ it retrieves user by ID (line 53)
    └─ UpdateAsync
       └─ ⚠️  it updates user data (line 67) [PENDING]

specs/AuthService.spec.csx
  AuthService
    └─ Login
       ├─ 🔍 it authenticates valid credentials (line 12) [FOCUSED]
       ├─ ⊘ it rejects invalid password (line 20) [SKIPPED]
       └─ ✓ it handles locked accounts (line 28)

Total: 9 specs (1 focused, 1 skipped, 1 pending)
Files: 2 spec files, 0 with compilation errors
```

### Flat Format

```
specs/UserService.spec.csx:15    UserService > CreateAsync > it creates a user with valid data
specs/UserService.spec.csx:23    UserService > CreateAsync > it validates email format
specs/UserService.spec.csx:31    UserService > CreateAsync > it rejects duplicate emails
specs/UserService.spec.csx:45    UserService > GetAsync > it returns null for missing user
specs/UserService.spec.csx:53    UserService > GetAsync > it retrieves user by ID
specs/UserService.spec.csx:67    UserService > UpdateAsync > it updates user data [PENDING]
specs/AuthService.spec.csx:12    AuthService > Login > it authenticates valid credentials [FOCUSED]
specs/AuthService.spec.csx:20    AuthService > Login > it rejects invalid password [SKIPPED]
specs/AuthService.spec.csx:28    AuthService > Login > it handles locked accounts

Total: 9 specs (1 focused, 1 skipped, 1 pending)
```

### JSON Format

```json
{
  "specs": [
    {
      "id": "specs/UserService.spec.csx:UserService/CreateAsync/creates a user with valid data",
      "description": "creates a user with valid data",
      "displayName": "UserService > CreateAsync > creates a user with valid data",
      "contextPath": ["UserService", "CreateAsync"],
      "sourceFile": "/absolute/path/specs/UserService.spec.csx",
      "relativeSourceFile": "specs/UserService.spec.csx",
      "lineNumber": 15,
      "type": "regular",
      "isPending": false,
      "isSkipped": false,
      "isFocused": false,
      "tags": ["database", "user-management"]
    }
  ],
  "summary": {
    "totalSpecs": 9,
    "focusedCount": 1,
    "skippedCount": 1,
    "pendingCount": 1,
    "filesWithErrors": 0,
    "totalFiles": 2
  }
}
```

### CSV Format

```csv
File,Line,Context Path,Description,Type,Tags
specs/UserService.spec.csx,15,UserService > CreateAsync,creates a user with valid data,regular,"database,user-management"
specs/UserService.spec.csx,23,UserService > CreateAsync,validates email format,regular,"validation"
specs/UserService.spec.csx,31,UserService > CreateAsync,rejects duplicate emails,regular,"validation,database"
specs/UserService.spec.csx,45,UserService > GetAsync,returns null for missing user,regular,"database"
specs/UserService.spec.csx,53,UserService > GetAsync,retrieves user by ID,regular,"database"
specs/UserService.spec.csx,67,UserService > UpdateAsync,updates user data,pending,""
specs/AuthService.spec.csx,12,AuthService > Login,authenticates valid credentials,focused,"auth"
specs/AuthService.spec.csx,20,AuthService > Login,rejects invalid password,skipped,"auth"
specs/AuthService.spec.csx,28,AuthService > Login,handles locked accounts,regular,"auth,security"
```

## Implementation

### 1. Add CLI Option

**File**: `src/DraftSpec.Cli/CliOptions.cs`

```csharp
public sealed class CliOptions
{
    // Existing properties...

    // List command options
    public bool List { get; set; }
    public bool ShowLineNumbers { get; set; }
    public bool FocusedOnly { get; set; }
    public bool PendingOnly { get; set; }
    public bool SkippedOnly { get; set; }
    public bool ShowCompilationErrors { get; set; }
    public string ListFormat { get; set; } = "tree"; // tree|flat|json|csv
    public string? OutputFile { get; set; }
}
```

### 2. Add Command Handler

**File**: `src/DraftSpec.Cli/Commands/ListCommand.cs`

```csharp
using System.Text.Json;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

public static class ListCommand
{
    public static async Task<int> Execute(CliOptions options)
    {
        // Discover all specs using static parsing
        var projectPath = Path.GetFullPath(options.Path);
        var discoverer = new SpecDiscoverer(projectPath);

        Console.WriteLine("Discovering specs...");
        var result = await discoverer.DiscoverAsync();

        // Apply filters
        var specs = result.Specs.AsEnumerable();

        if (options.FocusedOnly)
            specs = specs.Where(s => s.IsFocused);

        if (options.PendingOnly)
            specs = specs.Where(s => s.IsPending);

        if (options.SkippedOnly)
            specs = specs.Where(s => s.IsSkipped);

        if (!string.IsNullOrEmpty(options.FilterName))
            specs = specs.Where(s => s.Description.Contains(options.FilterName, StringComparison.OrdinalIgnoreCase));

        if (options.FilterTags?.Any() == true)
            specs = specs.Where(s => s.Tags.Any(t => options.FilterTags.Contains(t)));

        var specList = specs.ToList();

        // Format output
        var formatter = CreateFormatter(options.ListFormat, options);
        var output = formatter.Format(specList, result.Errors);

        // Write to file or console
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            await File.WriteAllTextAsync(options.OutputFile, output);
            Console.WriteLine($"Wrote {specList.Count} specs to {options.OutputFile}");
        }
        else
        {
            Console.WriteLine(output);
        }

        return 0;
    }

    private static IListFormatter CreateFormatter(string format, CliOptions options)
    {
        return format.ToLowerInvariant() switch
        {
            "tree" => new TreeListFormatter(options.ShowLineNumbers),
            "flat" => new FlatListFormatter(options.ShowLineNumbers),
            "json" => new JsonListFormatter(),
            "csv" => new CsvListFormatter(),
            _ => throw new ArgumentException($"Unknown format: {format}")
        };
    }
}
```

### 3. Implement Formatters

**File**: `src/DraftSpec.Cli/Formatters/IListFormatter.cs`

```csharp
namespace DraftSpec.Cli.Formatters;

public interface IListFormatter
{
    string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors);
}
```

**File**: `src/DraftSpec.Cli/Formatters/TreeListFormatter.cs`

```csharp
using System.Text;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

public sealed class TreeListFormatter : IListFormatter
{
    private readonly bool _showLineNumbers;

    public TreeListFormatter(bool showLineNumbers = false)
    {
        _showLineNumbers = showLineNumbers;
    }

    public string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var sb = new StringBuilder();

        // Group by file
        var byFile = specs.GroupBy(s => s.RelativeSourceFile)
                         .OrderBy(g => g.Key);

        foreach (var fileGroup in byFile)
        {
            sb.AppendLine(fileGroup.Key);

            // Build tree structure
            var tree = BuildTree(fileGroup.ToList());
            RenderTree(tree, sb, indent: 1);

            sb.AppendLine();
        }

        // Show errors
        if (errors.Any())
        {
            sb.AppendLine("Compilation Errors:");
            foreach (var error in errors)
            {
                sb.AppendLine($"  ❌ {error.RelativeSourceFile}");
                sb.AppendLine($"     {error.Message}");
            }
            sb.AppendLine();
        }

        // Summary
        var summary = GetSummary(specs, errors);
        sb.AppendLine(summary);

        return sb.ToString();
    }

    private TreeNode BuildTree(List<DiscoveredSpec> specs)
    {
        var root = new TreeNode { Description = "" };

        foreach (var spec in specs)
        {
            var current = root;

            // Navigate/create context nodes
            foreach (var context in spec.ContextPath)
            {
                var existing = current.Children.FirstOrDefault(c => c.Description == context);
                if (existing == null)
                {
                    existing = new TreeNode { Description = context };
                    current.Children.Add(existing);
                }
                current = existing;
            }

            // Add spec as leaf
            current.Specs.Add(spec);
        }

        return root;
    }

    private void RenderTree(TreeNode node, StringBuilder sb, int indent, bool isLast = false)
    {
        var prefix = new string(' ', indent * 2);

        // Render child contexts
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var isLastChild = (i == node.Children.Count - 1 && !node.Specs.Any());

            sb.AppendLine($"{prefix}{child.Description}");
            RenderTree(child, sb, indent + 1, isLastChild);
        }

        // Render specs
        for (int i = 0; i < node.Specs.Count; i++)
        {
            var spec = node.Specs[i];
            var isLastSpec = (i == node.Specs.Count - 1);
            var branch = isLastSpec ? "└─" : "├─";

            var icon = GetSpecIcon(spec);
            var lineInfo = _showLineNumbers ? $" (line {spec.LineNumber})" : "";
            var flags = GetSpecFlags(spec);

            sb.AppendLine($"{prefix}{branch} {icon} {spec.Description}{lineInfo}{flags}");
        }
    }

    private static string GetSpecIcon(DiscoveredSpec spec)
    {
        if (spec.IsFocused) return "🔍";
        if (spec.IsSkipped) return "⊘";
        if (spec.IsPending) return "⚠️";
        if (spec.HasCompilationError) return "❌";
        return "✓";
    }

    private static string GetSpecFlags(DiscoveredSpec spec)
    {
        var flags = new List<string>();
        if (spec.IsFocused) flags.Add("FOCUSED");
        if (spec.IsSkipped) flags.Add("SKIPPED");
        if (spec.IsPending) flags.Add("PENDING");
        if (spec.HasCompilationError) flags.Add("COMPILATION ERROR");

        return flags.Any() ? $" [{string.Join(", ", flags)}]" : "";
    }

    private static string GetSummary(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var focusedCount = specs.Count(s => s.IsFocused);
        var skippedCount = specs.Count(s => s.IsSkipped);
        var pendingCount = specs.Count(s => s.IsPending);
        var errorCount = specs.Count(s => s.HasCompilationError);
        var fileCount = specs.Select(s => s.RelativeSourceFile).Distinct().Count();

        return $@"Total: {specs.Count} specs ({focusedCount} focused, {skippedCount} skipped, {pendingCount} pending)
Files: {fileCount} spec files, {errors.Count} with compilation errors";
    }

    private sealed class TreeNode
    {
        public string Description { get; init; } = "";
        public List<TreeNode> Children { get; } = new();
        public List<DiscoveredSpec> Specs { get; } = new();
    }
}
```

**File**: `src/DraftSpec.Cli/Formatters/JsonListFormatter.cs`

```csharp
using System.Text.Json;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

public sealed class JsonListFormatter : IListFormatter
{
    public string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var output = new
        {
            specs = specs.Select(s => new
            {
                id = s.Id,
                description = s.Description,
                displayName = s.DisplayName,
                contextPath = s.ContextPath,
                sourceFile = s.SourceFile,
                relativeSourceFile = s.RelativeSourceFile,
                lineNumber = s.LineNumber,
                type = GetSpecType(s),
                isPending = s.IsPending,
                isSkipped = s.IsSkipped,
                isFocused = s.IsFocused,
                tags = s.Tags,
                compilationError = s.CompilationError
            }),
            summary = new
            {
                totalSpecs = specs.Count,
                focusedCount = specs.Count(s => s.IsFocused),
                skippedCount = specs.Count(s => s.IsSkipped),
                pendingCount = specs.Count(s => s.IsPending),
                filesWithErrors = errors.Count,
                totalFiles = specs.Select(s => s.RelativeSourceFile).Distinct().Count()
            },
            errors = errors.Select(e => new
            {
                file = e.RelativeSourceFile,
                message = e.Message
            })
        };

        return JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string GetSpecType(DiscoveredSpec spec)
    {
        if (spec.IsFocused) return "focused";
        if (spec.IsSkipped) return "skipped";
        if (spec.IsPending) return "pending";
        return "regular";
    }
}
```

### 4. Wire Up in CLI Entry Point

**File**: `src/DraftSpec.Cli/Program.cs`

```csharp
// Parse options
var options = CliOptionsParser.Parse(args);

// Route to command
if (options.List)
{
    return await ListCommand.Execute(options);
}
else if (options.Watch)
{
    return WatchCommand.Execute(options);
}
else
{
    return RunCommand.Execute(options);
}
```

## Testing

### Unit Tests

**File**: `tests/DraftSpec.Cli.Tests/Commands/ListCommandTests.cs`

```csharp
public class ListCommandTests
{
    [Fact]
    public async Task Execute_WithTreeFormat_ShowsHierarchy()
    {
        // Arrange
        var options = new CliOptions
        {
            Path = TestHelpers.GetTestSpecsPath(),
            List = true,
            ListFormat = "tree"
        };

        // Act
        var exitCode = await ListCommand.Execute(options);

        // Assert
        exitCode.Should().Be(0);
        // Verify output contains expected tree structure
    }

    [Fact]
    public async Task Execute_WithFocusedFilter_ShowsOnlyFocusedSpecs()
    {
        var options = new CliOptions
        {
            Path = TestHelpers.GetTestSpecsPath(),
            List = true,
            FocusedOnly = true
        };

        var exitCode = await ListCommand.Execute(options);

        exitCode.Should().Be(0);
        // Verify output contains only focused specs
    }
}
```

### Integration Tests

```bash
# Test with real spec files
draftspec list examples/TodoApi --format tree
draftspec list examples/TodoApi --format json > /tmp/specs.json
draftspec list examples/TodoApi --focused-only
draftspec list examples/TodoApi --filter "Todo"
```

## Usage Examples

### CI Pipeline Integration

**GitHub Actions**:

```yaml
- name: List Specs
  run: draftspec list . --format json --output specs.json

- name: Verify Spec Count
  run: |
    SPEC_COUNT=$(jq '.summary.totalSpecs' specs.json)
    if [ $SPEC_COUNT -lt 100 ]; then
      echo "Error: Expected at least 100 specs, found $SPEC_COUNT"
      exit 1
    fi
```

### Documentation Generation

```bash
# Generate markdown checklist
draftspec list . --format flat > FEATURE_CHECKLIST.md

# Add to PR template
gh pr create --body "$(cat FEATURE_CHECKLIST.md)"
```

### IDE Integration

```bash
# Get specs for file outline panel
draftspec list specs/UserService.spec.csx --format json
```

## Performance Considerations

- **Caching**: Cache parsed specs to avoid re-parsing on subsequent calls
- **Parallel Discovery**: Parse multiple files concurrently
- **Lazy Formatting**: Only format visible portion for large lists

## Future Enhancements

1. **Interactive Mode**: TUI with filtering and navigation
2. **Diff Mode**: Compare specs between branches (`draftspec list --diff main..HEAD`)
3. **Stats Mode**: Show spec count trends over time
4. **Export Formats**: Add JUnit XML, TeamCity, etc.
