# DSL Reference

Complete API reference for DraftSpec's describe/it/expect DSL.

## Import

```csharp
using static DraftSpec.Dsl;
```

This brings all DSL functions into scope: `describe`, `it`, `expect`, hooks, and `run`.

---

## Context Blocks

### describe(description, body)

Groups related specs. The first `describe` creates the root context; nested calls create child contexts.

```csharp
describe("Calculator", () =>
{
    describe("Add", () =>
    {
        it("sums numbers", () => { /* ... */ });
    });
});
```

### context(description, body)

Alias for `describe`. Use for sub-groupings when it reads better:

```csharp
describe("User", () =>
{
    context("when logged in", () =>
    {
        it("shows dashboard", () => { /* ... */ });
    });

    context("when logged out", () =>
    {
        it("redirects to login", () => { /* ... */ });
    });
});
```

---

## Specs

### it(description, body)

Defines a spec (test case). Supports both sync and async bodies.

**Sync:**
```csharp
it("adds numbers", () =>
{
    expect(1 + 1).toBe(2);
});
```

**Async:**
```csharp
it("fetches data", async () =>
{
    var result = await client.GetAsync("/api/users");
    expect(result.StatusCode).toBe(HttpStatusCode.OK);
});
```

### it(description)

Defines a **pending** spec (no implementation yet). Pending specs are reported but don't fail.

```csharp
it("handles edge case");  // Pending - reminds you to implement later
```

### fit(description, body)

**Focused** spec. When any `fit` exists, only focused specs run. Non-focused specs are skipped.

```csharp
fit("only this runs", () =>
{
    expect(true).toBeTrue();
});

it("this is skipped", () =>
{
    // Won't execute when fit exists
});
```

Supports both sync and async bodies.

### xit(description, body?)

**Skipped** spec. Explicitly disabled; reported as skipped.

```csharp
xit("temporarily disabled", () =>
{
    // Won't execute
});

xit("also skipped");  // Body is optional
```

---

## Hooks

Hooks run setup/teardown code around specs. All hooks support both sync and async.

### before(hook)

Runs **before each spec** in the current context and nested contexts.

```csharp
describe("Database", () =>
{
    before(() => { db.BeginTransaction(); });
    before(async () => { await db.BeginTransactionAsync(); });  // async

    it("inserts record", () => { /* ... */ });
    it("updates record", () => { /* ... */ });
});
```

### after(hook)

Runs **after each spec** in the current context and nested contexts.

```csharp
describe("Database", () =>
{
    after(() => { db.RollbackTransaction(); });

    it("inserts record", () => { /* ... */ });
});
```

### beforeAll(hook)

Runs **once before all specs** in the current context.

```csharp
describe("Integration tests", () =>
{
    beforeAll(() => { server.Start(); });
    beforeAll(async () => { await server.StartAsync(); });

    it("connects", () => { /* ... */ });
    it("sends request", () => { /* ... */ });
});
```

### afterAll(hook)

Runs **once after all specs** in the current context.

```csharp
describe("Integration tests", () =>
{
    afterAll(() => { server.Stop(); });

    it("test 1", () => { /* ... */ });
    it("test 2", () => { /* ... */ });
});
```

### Hook Execution Order

For nested contexts, hooks execute in this order:

```
beforeAll (outer)
beforeAll (inner)
  before (outer) → before (inner) → spec → after (inner) → after (outer)
afterAll (inner)
afterAll (outer)
```

Example:
```csharp
describe("Outer", () =>
{
    beforeAll(() => Console.WriteLine("1. beforeAll outer"));
    before(() => Console.WriteLine("2. before outer"));
    after(() => Console.WriteLine("5. after outer"));
    afterAll(() => Console.WriteLine("7. afterAll outer"));

    describe("Inner", () =>
    {
        beforeAll(() => Console.WriteLine("   beforeAll inner"));
        before(() => Console.WriteLine("3. before inner"));
        after(() => Console.WriteLine("4. after inner"));
        afterAll(() => Console.WriteLine("6. afterAll inner"));

        it("spec", () => Console.WriteLine("   --- spec ---"));
    });
});
```

---

## Tags

Tags categorize specs for filtering.

### tag(name, body)

Apply a single tag to specs within the block:

```csharp
tag("slow", () =>
{
    it("takes a while", () => { /* ... */ });  // has tag "slow"
});
```

### tags(names, body)

Apply multiple tags to specs within the block:

```csharp
tags(["integration", "database"], () =>
{
    it("connects to database", () => { /* ... */ });  // has both tags
});
```

### Nested Tags

Tags accumulate when nested:

```csharp
tag("slow", () =>
{
    tag("integration", () =>
    {
        it("test", () => { });  // has both "slow" and "integration"
    });
});
```

### Filtering by Tag

Use `configure()` to filter specs by tag:

```csharp
// Run only specs with "fast" tag
configure(runner => runner.WithTagFilter("fast"));

// Run specs with either "unit" or "fast" tag
configure(runner => runner.WithTagFilter("unit", "fast"));

// Exclude specs with "slow" tag
configure(runner => runner.WithoutTags("slow"));
```

---

## Configuration

### configure(Action\<SpecRunnerBuilder\>)

Configure the spec runner with middleware. Call before `run()`.

```csharp
configure(runner => runner
    .WithRetry(3)
    .WithTimeout(5000)
    .WithParallelExecution()
);
```

**Available options:**

| Method | Description |
|--------|-------------|
| `WithRetry(n)` | Retry failed specs up to n times |
| `WithRetry(n, delayMs)` | Retry with delay between attempts |
| `WithTimeout(ms)` | Fail specs that exceed timeout |
| `WithFilter(predicate)` | Custom filter function |
| `WithNameFilter(pattern)` | Filter by regex pattern on description |
| `WithTagFilter(tags...)` | Run only specs with any of these tags |
| `WithoutTags(tags...)` | Exclude specs with any of these tags |
| `WithParallelExecution()` | Run specs in parallel |
| `WithParallelExecution(n)` | Run with max n parallel specs |
| `Use(middleware)` | Add custom middleware |

### configure(Action\<DraftSpecConfiguration\>)

Configure DraftSpec with plugins, formatters, and reporters:

```csharp
configure(config =>
{
    config.UsePlugin<SlackReporterPlugin>();
    config.AddReporter(new FileReporter("results.json"));
    config.AddFormatter("custom", new MyFormatter());
});
```

See [Configuration](configuration.md) for details.

---

## Running Specs

### run(json = false)

Execute all specs and output results. Call at the end of your spec file.

```csharp
describe("MyTests", () =>
{
    it("works", () => expect(true).toBeTrue());
});

run();  // Console output
```

**JSON output:**
```csharp
run(json: true);  // JSON format for tooling
```

**Exit code:** Sets `Environment.ExitCode` to 1 if any specs failed.

---

## Complete Example

```csharp
#r "nuget: DraftSpec"
using static DraftSpec.Dsl;

var db = new TestDatabase();

configure(runner => runner
    .WithTimeout(5000)
    .WithRetry(2)
);

describe("UserRepository", () =>
{
    beforeAll(() => db.Connect());
    afterAll(() => db.Disconnect());

    before(() => db.BeginTransaction());
    after(() => db.Rollback());

    describe("Create", () =>
    {
        it("inserts a user", () =>
        {
            var user = new User("test@example.com");
            db.Users.Add(user);
            expect(user.Id).toNotBeNull();
        });

        it("validates email", () =>
        {
            expect(() => new User("invalid"))
                .toThrow<ArgumentException>();
        });

        tag("slow", () =>
        {
            it("handles bulk insert", async () =>
            {
                var users = Enumerable.Range(0, 1000)
                    .Select(i => new User($"user{i}@test.com"));
                await db.Users.AddRangeAsync(users);
                expect(db.Users.Count()).toBe(1000);
            });
        });
    });

    describe("Delete", () =>
    {
        it("removes user");  // pending

        xit("cascades to orders", () =>
        {
            // skipped for now
        });
    });
});

run();
```

---

## See Also

- **[Assertions](assertions.md)** - Full `expect()` API
- **[CLI Reference](cli.md)** - Command-line options
- **[Configuration](configuration.md)** - Plugins and middleware
