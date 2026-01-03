# Troubleshooting

Common issues and solutions when using DraftSpec.

## Table of Contents

- [CLI Issues](#cli-issues)
- [Spec Discovery Issues](#spec-discovery-issues)
- [Compilation Errors](#compilation-errors)
- [Execution Issues](#execution-issues)
- [Assertion Issues](#assertion-issues)
- [Hook Issues](#hook-issues)
- [Focus/Skip Issues](#focusskip-issues)
- [IDE Issues](#ide-issues)
- [Performance Issues](#performance-issues)
- [Cache Issues](#cache-issues)
- [MTP Integration Issues](#mtp-integration-issues)

---

## CLI Issues

### "draftspec: command not found"

The CLI tool isn't installed or not in PATH.

```bash
# Install globally
dotnet tool install -g DraftSpec.Cli --prerelease

# Verify installation
dotnet tool list -g | grep draftspec
```

If installed but not found, add `~/.dotnet/tools` to your PATH:

```bash
# Bash/Zsh
export PATH="$PATH:$HOME/.dotnet/tools"

# Add to ~/.bashrc or ~/.zshrc for persistence
```

### "Could not load file or assembly 'DraftSpec'"

Version mismatch between CLI and spec file.

```bash
# Update CLI to latest
dotnet tool update -g DraftSpec.Cli --prerelease

# Or pin version in spec file
#r "nuget: DraftSpec, 0.7.0"
```

### Specs run but no output appears

Check your formatter configuration:

```bash
# Explicitly set console output
draftspec run . --format console
```

Also check if output is being redirected:

```bash
# Verify nothing is capturing stdout
draftspec run . 2>&1
```

### "Access denied" or permission errors

On macOS/Linux, ensure the tool is executable:

```bash
chmod +x ~/.dotnet/tools/draftspec
```

On Windows, run terminal as administrator or check antivirus blocking.

---

## Spec Discovery Issues

### "No specs found"

1. **Check file extension**: Must be `.spec.csx`
2. **Check file location**: Run from correct directory
3. **Check for syntax errors**: Run with verbose output

```bash
draftspec run ./specs --verbose

# List files that would be discovered
draftspec list .
```

### Specs not discovered in subdirectories

DraftSpec searches recursively by default. Check:

```bash
# Verify files exist
find . -name "*.spec.csx"

# Check if directory is excluded
ls -la  # Look for .gitignore patterns
```

### #load directive not resolving

The `#load` directive paths are relative to the spec file:

```csharp
// In specs/user/UserService.spec.csx

// Wrong: Relative to cwd
#load "spec_helper.csx"

// Correct: Relative to this file
#load "../spec_helper.csx"
```

### Specs not discovered by `dotnet test`

1. Ensure `DraftSpec.TestingPlatform` package is installed
2. Check that spec files are included in the project:

```xml
<ItemGroup>
  <None Include="**/*.spec.csx" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

3. Verify the test adapter is registered in your `.csproj`
4. Check that the spec files are copied to output directory

---

## Compilation Errors

### "CS0103: The name 'describe' does not exist"

Missing DraftSpec import. Add to your spec file:

```csharp
#r "nuget: DraftSpec"
using static DraftSpec.Dsl;
```

Or ensure `spec_helper.csx` includes these and you're loading it:

```csharp
#load "spec_helper.csx"
```

### "error CS1061: 'Type' does not contain a definition for..."

Your project DLL might not be built or referenced correctly:

```bash
# Build project first
dotnet build

# Then run specs
draftspec run .
```

Check `spec_helper.csx` has correct DLL path:

```csharp
#r "bin/Debug/net10.0/MyProject.dll"  // Adjust path as needed
```

### NuGet package restore failures

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Try running specs again
draftspec run .
```

### "Could not load type 'X' from assembly 'Y'"

Assembly version mismatch. Ensure consistent versions:

```csharp
// Specify exact version
#r "nuget: DraftSpec, 0.7.0"
#r "nuget: SomePackage, 1.2.3"
```

### Cache corruption

If you see unexpected compilation errors after upgrading:

```bash
# Clear the compilation cache
draftspec cache clear

# Run specs fresh
draftspec run . --no-cache
```

---

## Execution Issues

### Async specs timing out

Default timeout is 30 seconds. Increase if needed:

```bash
# Via CLI
draftspec run . --timeout 60000  # 60 seconds

# Via config file (draftspec.json)
{
  "timeout": 60000
}
```

Check for deadlocks in async code:

```csharp
// DON'T: .Result or .Wait() can deadlock
var result = service.GetAsync().Result;

// DO: use await
var result = await service.GetAsync();
```

### "Cannot access a disposed object"

Async disposal timing issue. Ensure proper lifecycle:

```csharp
describe("Database", () =>
{
    IDbConnection? db = null;

    beforeAll(async () =>
    {
        db = await CreateConnectionAsync();
    });

    afterAll(async () =>
    {
        if (db != null)
            await db.DisposeAsync();
    });

    it("queries data", async () =>
    {
        // db is guaranteed to be initialized here
        var result = await db!.QueryAsync("SELECT 1");
        expect(result).toNotBeNull();
    });
});
```

### Parallel execution conflicts

Shared state between specs causes race conditions:

```csharp
describe("Counter", () =>
{
    // BAD: Shared mutable state
    static int count = 0;

    it("test 1", () => count++);  // Race condition!
    it("test 2", () => count++);
});

// GOOD: Per-spec state
describe("Counter", () =>
{
    int count = 0;

    before(() => count = 0);  // Reset for each spec

    it("test 1", () =>
    {
        count++;
        expect(count).toBe(1);
    });
});
```

Disable parallel execution if needed:

```bash
draftspec run . --no-parallel
```

### Specs hang indefinitely

Common causes:
1. Deadlocked async code
2. Infinite loops
3. Waiting for external resources

Use timeout to fail fast:

```bash
draftspec run . --timeout 10000  # 10 second timeout
```

Debug by isolating the hanging spec:

```bash
# Run single file
draftspec run ./specs/problematic.spec.csx
```

---

## Assertion Issues

### "Expected X to be Y, but was Y"

Usually a reference vs value comparison issue:

```csharp
// For objects, toBe() uses reference equality
expect(result).toBe(expected);  // Fails if different instances

// Options:
// 1. Compare individual properties
expect(result.Name).toBe(expected.Name);

// 2. Use toBeEquivalentTo() for deep comparison
expect(result).toBeEquivalentTo(expected);
```

### Collection assertions not matching

Order matters with `toBe()`:

```csharp
// toBe() requires same order
expect([1, 2, 3]).toBe([1, 2, 3]);  // Pass
expect([1, 2, 3]).toBe([3, 2, 1]);  // Fail!

// Use toContainExactly() for order-independent comparison
expect([1, 2, 3]).toContainExactly([3, 2, 1]);  // Pass
```

### Floating point comparisons failing

Use `toBeCloseTo()` for floating point:

```csharp
// Fails due to floating point precision
expect(0.1 + 0.2).toBe(0.3);

// Works with tolerance
expect(0.1 + 0.2).toBeCloseTo(0.3, 0.0001);
```

### Exception not being caught

Use the correct async variant:

```csharp
// Sync action
expect(() => ThrowSync()).toThrow<InvalidOperationException>();

// Async action - use toThrowAsync
expect(async () => await ThrowAsync()).toThrowAsync<InvalidOperationException>();
```

### Assertion message unclear

Read the full message for context:

```
Expected user.Email to be "john@example.com", but was "JOHN@EXAMPLE.COM"
```

The message shows both the expression and values.

---

## Hook Issues

### Hooks running in wrong order

DraftSpec follows RSpec hook order:

1. `beforeAll` (once per context)
2. `before` (parent to child, before each spec)
3. **spec runs**
4. `after` (child to parent, after each spec)
5. `afterAll` (once per context)

```csharp
describe("Parent", () =>
{
    beforeAll(() => Console.WriteLine("1. Parent beforeAll"));
    before(() => Console.WriteLine("2. Parent before"));

    describe("Child", () =>
    {
        before(() => Console.WriteLine("3. Child before"));
        after(() => Console.WriteLine("4. Child after"));

        it("spec", () => Console.WriteLine("   spec runs"));
    });

    after(() => Console.WriteLine("5. Parent after"));
    afterAll(() => Console.WriteLine("6. Parent afterAll"));
});
```

### beforeAll/afterAll running multiple times

This happens with parallel execution. Use thread-safe initialization:

```csharp
describe("Database", () =>
{
    static readonly Lazy<IDbConnection> _db = new(() => CreateConnection());

    IDbConnection db => _db.Value;

    it("test 1", () => { /* uses db */ });
    it("test 2", () => { /* uses db */ });
});
```

### Shared state between specs

Hooks don't automatically reset state. Use `before` to initialize:

```csharp
describe("Counter", () =>
{
    int count = 0;  // Shared - dangerous!

    before(() => count = 0);  // Reset before each spec

    it("increments", () =>
    {
        count++;
        expect(count).toBe(1);
    });

    it("also increments", () =>
    {
        count++;
        expect(count).toBe(1);  // Works because before() reset it
    });
});
```

### Hook exceptions not reported

If a hook throws, it affects all specs in that context. Check for:

```csharp
beforeAll(() =>
{
    // This exception might be swallowed or reported differently
    throw new Exception("Setup failed");
});
```

Run with verbose output to see hook errors:

```bash
draftspec run . --verbose
```

---

## Focus/Skip Issues

### All specs skipped unexpectedly

Check for `fit()` somewhere in your codebase. When any spec is focused, all non-focused specs are skipped.

```bash
# Find focused specs
grep -r "fit(" ./specs

# Or use list command
draftspec list . --focused-only
```

### Focus/skip not working

Ensure you're using the correct DSL:

```csharp
fit("focused", () => { });   // Focus
xit("skipped", () => { });   // Skip
it("normal");                // Pending (no body)
```

### Pending specs not showing in output

Pending specs (specs without a body) are reported differently by formatters. Use `--format json` to see all specs:

```bash
draftspec run . --format json | jq '.specs[] | select(.status == "pending")'

# Or use list command
draftspec list . --pending-only
```

---

## IDE Issues

### CSX files not recognized

1. Install OmniSharp extension (VS Code) or enable CSX support (Rider)
2. Create `omnisharp.json` in project root:

```json
{
  "script": {
    "enableScriptNuGetReferences": true
  }
}
```

Or run:

```bash
draftspec init
```

### IntelliSense not working in spec files

1. Ensure the `#r "nuget: DraftSpec, *"` directive is at the top
2. Reload the IDE window
3. Check OmniSharp logs for errors

For VS Code:
```
View > Output > OmniSharp Log
```

### Test Explorer not showing specs

For MTP integration:
1. Ensure `DraftSpec.TestingPlatform` package is installed
2. Build the project: `dotnet build`
3. Refresh test explorer
4. Check that spec files are copied to output directory

### Debugging specs not working

Set breakpoints in your spec code, then:

**VS Code:**
```json
// .vscode/launch.json
{
  "configurations": [
    {
      "name": "Debug Specs",
      "type": "coreclr",
      "request": "launch",
      "program": "dotnet",
      "args": ["test", "--no-build"],
      "cwd": "${workspaceFolder}"
    }
  ]
}
```

**Rider/Visual Studio:**
Right-click test in Test Explorer > Debug

---

## Performance Issues

### Slow spec execution

1. **Enable parallel execution**:
   ```bash
   draftspec run . --parallel
   ```

2. **Use `beforeAll` for expensive setup**:
   ```csharp
   describe("Database", () =>
   {
       // DON'T: Create connection for each spec
       // before(() => db = CreateConnection());

       // DO: Create once, reuse
       beforeAll(() => db = CreateConnection());
       afterAll(() => db.Dispose());
   });
   ```

3. **Profile with verbose output**:
   ```bash
   draftspec run . --verbose
   ```

### Slow spec discovery

Static parsing should be fast. If discovery is slow:

1. **Check for complex #load chains** - deeply nested includes slow parsing
2. **Reduce file count** - consolidate small spec files
3. **Use watch mode** - only re-parses changed files

```bash
draftspec watch .
```

### Watch mode recompiling too often

Narrow the watch scope:

```bash
# Watch only specific directory
draftspec watch ./specs

# Exclude generated files
# Add to .gitignore or create .draftspecignore
```

---

## Cache Issues

### Stale cache causing issues

Clear the cache:

```bash
# Clear all cached data
draftspec cache clear

# Or run without cache
draftspec run . --no-cache
```

### Cache taking too much disk space

Prune old entries:

```bash
# Show cache stats
draftspec cache stats

# Remove stale entries
draftspec cache prune
```

### Cache location

Default cache location:
- **Project**: `.draftspec/` directory in project root
- **User**: `~/.draftspec/` for global cache

---

## MTP Integration Issues

### Tests not appearing in Test Explorer

1. Ensure package reference is correct:
   ```xml
   <PackageReference Include="DraftSpec.TestingPlatform" Version="0.7.0-*" />
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Spec files must be in output directory:
   ```xml
   <None Include="**/*.spec.csx" CopyToOutputDirectory="PreserveNewest" />
   ```

### "dotnet test" not finding tests

Check test adapter is loaded:

```bash
dotnet test --list-tests
```

If no tests listed, verify:
1. Package is installed
2. Project builds successfully
3. Spec files are in expected location

### Coverage not working with MTP

Use the built-in coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Results are in `TestResults/` directory.

---

## Still stuck?

1. Check [existing issues](https://github.com/juvistr/draftspec/issues)
2. Ask in [Discussions](https://github.com/juvistr/draftspec/discussions)
3. [Open a bug report](https://github.com/juvistr/draftspec/issues/new?template=bug_report.md)

When reporting issues, include:
- DraftSpec version (`draftspec --version`)
- .NET version (`dotnet --version`)
- Operating system
- Minimal reproduction case
- Full error message and stack trace
