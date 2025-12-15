using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService
{
    private readonly ITodoRepository _repository;

    public TodoService(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Todo> CreateAsync(string title, Priority priority = Priority.Medium, DateTime? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Title cannot be empty");

        var todo = new Todo
        {
            Title = title.Trim(),
            Priority = priority,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(todo);
    }

    public Task<Todo?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<IReadOnlyList<Todo>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    public async Task<IReadOnlyList<Todo>> GetByPriorityAsync(Priority priority)
    {
        var all = await _repository.GetAllAsync();
        return all.Where(t => t.Priority == priority).ToList();
    }

    public async Task<Todo> CompleteAsync(int id)
    {
        var todo = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Todo {id} not found");

        var completed = todo with { IsComplete = true };
        return await _repository.UpdateAsync(completed);
    }

    public async Task<Todo> AssignToUserAsync(int todoId, int userId)
    {
        var todo = await _repository.GetByIdAsync(todoId)
            ?? throw new InvalidOperationException($"Todo {todoId} not found");

        var assigned = todo with { AssignedUserId = userId };
        return await _repository.UpdateAsync(assigned);
    }

    public async Task<Todo> AddTagAsync(int todoId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ValidationException("Tag cannot be empty");

        var todo = await _repository.GetByIdAsync(todoId)
            ?? throw new InvalidOperationException($"Todo {todoId} not found");

        if (todo.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            return todo;

        var tagged = todo with { Tags = [.. todo.Tags, tag] };
        return await _repository.UpdateAsync(tagged);
    }

    public async Task<IReadOnlyList<Todo>> GetOverdueAsync()
    {
        var all = await _repository.GetAllAsync();
        var now = DateTime.UtcNow;
        return all.Where(t => t.DueDate.HasValue && t.DueDate < now && !t.IsComplete).ToList();
    }

    public async Task<IReadOnlyList<Todo>> GetByTagAsync(string tag)
    {
        var all = await _repository.GetAllAsync();
        return all.Where(t => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public Task DeleteAsync(int id)
    {
        return _repository.DeleteAsync(id);
    }
}
