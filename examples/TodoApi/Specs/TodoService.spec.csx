#load "../spec_helper.csx"
using static DraftSpec.Dsl;
using TodoApi.Models;
using TodoApi.Services;

// ============================================================================
// TodoService Specs - Real-World Usage Patterns
// ============================================================================
// This file demonstrates realistic testing patterns using DraftSpec.
// ============================================================================

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

        it("assigns sequential IDs", async () =>
        {
            var first = await service.CreateAsync("First");
            var second = await service.CreateAsync("Second");

            expect(second.Id).toBeGreaterThan(first.Id);
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

        it("sets CreatedAt to current time", async () =>
        {
            var before = DateTime.UtcNow;
            var todo = await service.CreateAsync("Timestamped");
            var after = DateTime.UtcNow;

            expect(todo.CreatedAt).toBeAtLeast(before);
            expect(todo.CreatedAt).toBeAtMost(after);
        });

        it("starts as incomplete", async () =>
        {
            var todo = await service.CreateAsync("New task");

            expect(todo.IsComplete).toBeFalse();
        });

        context("with due date", () =>
        {
            it("stores the due date", async () =>
            {
                var dueDate = DateTime.UtcNow.AddDays(7);
                var todo = await service.CreateAsync("Due next week", dueDate: dueDate);

                expect(todo.DueDate).toBe(dueDate);
            });
        });

        context("validation", () =>
        {
            it("throws on empty title", async () =>
            {
                await expect(async () => await service.CreateAsync(""))
                    .toThrowAsync<ValidationException>();
            });

            it("throws on whitespace-only title", async () =>
            {
                await expect(async () => await service.CreateAsync("   "))
                    .toThrowAsync<ValidationException>();
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

    describe("GetAllAsync", () =>
    {
        before(async () =>
        {
            // Create test data - uses 'before' since parent 'before' recreates service
            await service.CreateAsync("Task 1");
            await service.CreateAsync("Task 2");
            await service.CreateAsync("Task 3");
        });

        it("returns all todos", async () =>
        {
            var todos = await service.GetAllAsync();

            expect(todos.Count).toBeAtLeast(3);
        });
    });

    describe("GetByPriorityAsync", () =>
    {
        before(async () =>
        {
            await service.CreateAsync("Low 1", Priority.Low);
            await service.CreateAsync("High 1", Priority.High);
            await service.CreateAsync("Low 2", Priority.Low);
            await service.CreateAsync("High 2", Priority.High);
        });

        it("filters by priority", async () =>
        {
            var highPriority = await service.GetByPriorityAsync(Priority.High);

            expect(highPriority.Count).toBe(2);
            expect(highPriority.All(t => t.Priority == Priority.High)).toBeTrue();
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

        it("throws for non-existent todo", async () =>
        {
            await expect(async () => await service.CompleteAsync(9999))
                .toThrowAsync<InvalidOperationException>();
        });
    });

    describe("AssignToUserAsync", () =>
    {
        it("assigns todo to user", async () =>
        {
            var todo = await service.CreateAsync("Assign me");
            expect(todo.AssignedUserId).toBeNull();

            var assigned = await service.AssignToUserAsync(todo.Id, 42);

            expect(assigned.AssignedUserId).toBe(42);
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

        it("throws on empty tag", async () =>
        {
            var todo = await service.CreateAsync("Bad tag");

            await expect(async () => await service.AddTagAsync(todo.Id, ""))
                .toThrowAsync<ValidationException>();
        });
    });

    describe("RemoveTagAsync", () =>
    {
        it("removes the specified tag");

        it("does nothing if tag not present");

        it("is case-insensitive");
    });

    describe("GetOverdueAsync", () =>
    {
        before(async () =>
        {
            // Past due, incomplete
            var overdue = await service.CreateAsync("Overdue",
                dueDate: DateTime.UtcNow.AddDays(-1));

            // Past due but complete
            var completedOverdue = await service.CreateAsync("Completed overdue",
                dueDate: DateTime.UtcNow.AddDays(-1));
            await service.CompleteAsync(completedOverdue.Id);

            // Future due
            await service.CreateAsync("Future",
                dueDate: DateTime.UtcNow.AddDays(7));

            // No due date
            await service.CreateAsync("No due date");
        });

        it("returns only incomplete todos with past due dates", async () =>
        {
            var overdue = await service.GetOverdueAsync();

            expect(overdue.Count).toBe(1);
            expect(overdue[0].Title).toBe("Overdue");
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
