using TodoApi.Models;

namespace TodoApi.Services;

public class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<Todo> _todos = [];
    private int _nextId = 1;

    public Task<Todo> AddAsync(Todo todo)
    {
        var newTodo = todo with { Id = _nextId++ };
        _todos.Add(newTodo);
        return Task.FromResult(newTodo);
    }

    public Task<Todo?> GetByIdAsync(int id)
    {
        return Task.FromResult(_todos.FirstOrDefault(t => t.Id == id));
    }

    public Task<IReadOnlyList<Todo>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Todo>>(_todos.ToList());
    }

    public Task<Todo> UpdateAsync(Todo todo)
    {
        var index = _todos.FindIndex(t => t.Id == todo.Id);
        if (index < 0)
            throw new InvalidOperationException($"Todo {todo.Id} not found");
        _todos[index] = todo;
        return Task.FromResult(todo);
    }

    public Task DeleteAsync(int id)
    {
        _todos.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }

    public Task<int> GetNextIdAsync()
    {
        return Task.FromResult(_nextId);
    }

    public void Clear()
    {
        _todos.Clear();
        _nextId = 1;
    }
}
