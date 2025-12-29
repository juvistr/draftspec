#load "../spec_helper.csx"
using static DraftSpec.Dsl;
using TodoApi.Models;
using TodoApi.Services;

// ============================================================================
// Hook Execution Order Demo
// ============================================================================
// This file demonstrates the order in which hooks execute.
// Run it to see the execution flow printed to the console.
// ============================================================================

var executionLog = new List<string>();

void Log(string message) => executionLog.Add(message);

describe("Hook Execution Order", () =>
{
    beforeAll(() => Log("1. OUTER beforeAll"));
    afterAll(() =>
    {
        Log("8. OUTER afterAll");

        // Print the execution log at the very end
        Console.WriteLine("\n========================================");
        Console.WriteLine("Hook Execution Order:");
        Console.WriteLine("========================================");
        foreach (var entry in executionLog)
        {
            Console.WriteLine($"  {entry}");
        }
        Console.WriteLine("========================================\n");
    });

    before(() => Log("2. OUTER before (runs for each spec)"));
    after(() => Log("7. OUTER after (runs for each spec)"));

    describe("Nested Context", () =>
    {
        beforeAll(() => Log("3. INNER beforeAll"));
        afterAll(() => Log("6. INNER afterAll"));

        before(() => Log("4. INNER before"));
        after(() => Log("5. INNER after"));

        it("demonstrates hook nesting", () =>
        {
            Log("   >>> SPEC EXECUTES <<<");
            expect(true).toBeTrue();
        });
    });
});

// ============================================================================
// Expected output order for a single spec:
// ============================================================================
// 1. OUTER beforeAll           (once, at start of outer context)
// 3. INNER beforeAll           (once, at start of inner context)
// 2. OUTER before              (before each spec, parent first)
// 4. INNER before              (before each spec, child second)
//    >>> SPEC EXECUTES <<<
// 5. INNER after               (after each spec, child first)
// 7. OUTER after               (after each spec, parent second)
// 6. INNER afterAll            (once, at end of inner context)
// 8. OUTER afterAll            (once, at end of outer context)
// ============================================================================

describe("Multiple Specs Hook Order", () =>
{
    var specLog = new List<string>();

    beforeAll(() => specLog.Add("beforeAll"));
    afterAll(() =>
    {
        specLog.Add("afterAll");
        Console.WriteLine($"\nMultiple specs hook sequence: {string.Join(" -> ", specLog)}");
    });

    before(() => specLog.Add("before"));
    after(() => specLog.Add("after"));

    it("first spec", () => specLog.Add("spec1"));
    it("second spec", () => specLog.Add("spec2"));
    it("third spec", () => specLog.Add("spec3"));

    // Expected: beforeAll -> before -> spec1 -> after -> before -> spec2 -> after -> before -> spec3 -> after -> afterAll
});

describe("Async Hooks Order", () =>
{
    var asyncLog = new List<string>();

    beforeAll(async () =>
    {
        await Task.Delay(1);
        asyncLog.Add("async beforeAll");
    });

    afterAll(async () =>
    {
        await Task.Delay(1);
        asyncLog.Add("async afterAll");
        Console.WriteLine($"\nAsync hooks sequence: {string.Join(" -> ", asyncLog)}");
    });

    before(async () =>
    {
        await Task.Delay(1);
        asyncLog.Add("async before");
    });

    after(async () =>
    {
        await Task.Delay(1);
        asyncLog.Add("async after");
    });

    it("async hooks work the same as sync", async () =>
    {
        await Task.Delay(1);
        asyncLog.Add("async spec");
        expect(true).toBeTrue();
    });
});
