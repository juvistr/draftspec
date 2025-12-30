# Troubleshooting

Common issues and solutions when using DraftSpec.

## CLI Issues

### "draftspec: command not found"

The CLI tool isn't installed or not in PATH.

```bash
# Install globally
dotnet tool install -g DraftSpec.Cli --prerelease

# Verify installation
dotnet tool list -g | grep draftspec
```

If installed but not found, add `~/.dotnet/tools` to your PATH.

### "Could not load file or assembly 'DraftSpec'"

Version mismatch between CLI and spec file.

```bash
# Update CLI to latest
dotnet tool update -g DraftSpec.Cli --prerelease

# Or pin version in spec file
#r "nuget: DraftSpec, 0.4.0"
```

### Specs run but no output appears

Check your formatter configuration:

```bash
# Explicitly set console output
draftspec run . --format console
```

## Spec Discovery Issues

### "No specs found"

1. **Check file extension**: Must be `.spec.csx`
2. **Check file location**: Run from correct directory
3. **Check for syntax errors**: Run with verbose output

```bash
draftspec run ./specs --verbose
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

## Assertion Issues

### "Expected X to be Y, but was Y"

Usually a reference vs value comparison issue:

```csharp
// This may fail for objects
expect(result).toBe(expected);

// For complex objects, check individual properties
expect(result.Name).toBe(expected.Name);
```

### Async assertions timing out

Increase timeout or check for deadlocks:

```csharp
// Default timeout is 30 seconds
// Check if you're blocking on async code
it("async test", async () =>
{
    // DON'T: .Result or .Wait() can deadlock
    // var result = service.GetAsync().Result;

    // DO: use await
    var result = await service.GetAsync();
    expect(result).toNotBeNull();
});
```

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

## Focus/Skip Issues

### All specs skipped unexpectedly

Check for `fit()` somewhere in your codebase. When any spec is focused, all non-focused specs are skipped.

```bash
# Find focused specs
grep -r "fit(" ./specs
```

### Pending specs not showing in output

Pending specs (specs without a body) are reported differently by formatters. Use `--format json` to see all specs:

```bash
draftspec run . --format json | jq '.specs[] | select(.status == "pending")'
```

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

### IntelliSense not working in spec files

1. Ensure the `#r "nuget: DraftSpec, *"` directive is at the top
2. Reload the IDE window
3. Check OmniSharp logs for errors

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

## Still stuck?

1. Check [existing issues](https://github.com/juvistr/draftspec/issues)
2. Ask in [Discussions](https://github.com/juvistr/draftspec/discussions)
3. [Open a bug report](https://github.com/juvistr/draftspec/issues/new?template=bug_report.md)
