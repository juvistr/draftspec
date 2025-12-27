// TodoService.spec.csx - MTP Integration Example
// Run with: dotnet test
//
// This file demonstrates DraftSpec with Microsoft Testing Platform integration.
// Key differences from CLI-based specs:
// - No run() call needed (MTP handles execution)
// - Full IDE Test Explorer support
// - Click-to-navigate from test results to source

#load "spec_helper.csx"
using static DraftSpec.Dsl;
using TodoApi.Models;
using TodoApi.Services;

InMemoryTodoRepository repo = null!;
TodoService service = null!;

describe("TodoService", () =>
{
    before(() =>
    {
        repo = CreateRepository();
        service = CreateService(repo);
    });

    describe("CreateAsync", () =>
    {
        it("creates a todo with the given title", async () =>
        {
            var todo = await service.CreateAsync("Buy groceries");

            expect(todo.Title).toBe("Buy groceries");
            expect(todo.Id).toBeGreaterThan(0);
        });

        it("defaults to Medium priority", async () =>
        {
            var todo = await service.CreateAsync("Default priority");

            expect(todo.Priority).toBe(Priority.Medium);
        });

        it("accepts custom priority", async () =>
        {
            var todo = await service.CreateAsync("Urgent task", Priority.Critical);

            expect(todo.Priority).toBe(Priority.Critical);
        });

        it("starts as incomplete", async () =>
        {
            var todo = await service.CreateAsync("New task");

            expect(todo.IsComplete).toBeFalse();
        });

        context("validation", () =>
        {
            it("throws on empty title", () =>
            {
                expect(() => service.CreateAsync("").GetAwaiter().GetResult())
                    .toThrow<ValidationException>();
            });

            it("trims whitespace from title", async () =>
            {
                var todo = await service.CreateAsync("  padded title  ");

                expect(todo.Title).toBe("padded title");
            });
        });
    });

    describe("GetByIdAsync", () =>
    {
        it("returns the todo with matching ID", async () =>
        {
            var created = await service.CreateAsync("Find me");

            var found = await service.GetByIdAsync(created.Id);

            expect(found).toNotBeNull();
            expect(found!.Title).toBe("Find me");
        });

        it("returns null for non-existent ID", async () =>
        {
            var found = await service.GetByIdAsync(9999);

            expect(found).toBeNull();
        });
    });

    describe("CompleteAsync", () =>
    {
        it("marks todo as complete", async () =>
        {
            var todo = await service.CreateAsync("Complete me");
            expect(todo.IsComplete).toBeFalse();

            var completed = await service.CompleteAsync(todo.Id);

            expect(completed.IsComplete).toBeTrue();
        });

        it("throws for non-existent todo", () =>
        {
            expect(() => service.CompleteAsync(9999).GetAwaiter().GetResult())
                .toThrow<InvalidOperationException>();
        });
    });

    describe("AddTagAsync", () =>
    {
        it("adds a tag to the todo", async () =>
        {
            var todo = await service.CreateAsync("Tag me");

            var tagged = await service.AddTagAsync(todo.Id, "urgent");

            expect(tagged.Tags).toContain("urgent");
        });

        it("preserves existing tags", async () =>
        {
            var todo = await service.CreateAsync("Multi-tag");
            await service.AddTagAsync(todo.Id, "first");
            var result = await service.AddTagAsync(todo.Id, "second");

            expect(result.Tags).toContainAll("first", "second");
        });

        it("ignores duplicate tags (case-insensitive)", async () =>
        {
            var todo = await service.CreateAsync("Duplicate tag test");
            await service.AddTagAsync(todo.Id, "important");
            var result = await service.AddTagAsync(todo.Id, "IMPORTANT");

            expect(result.Tags).toHaveCount(1);
        });
    });

    describe("DeleteAsync", () =>
    {
        it("removes the todo", async () =>
        {
            var todo = await service.CreateAsync("Delete me");

            await service.DeleteAsync(todo.Id);

            var found = await service.GetByIdAsync(todo.Id);
            expect(found).toBeNull();
        });
    });
});

// Note: No run() call needed - MTP handles test execution
