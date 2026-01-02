namespace TodoApi.Models;

public record Todo
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public bool IsComplete { get; init; }
    public Priority Priority { get; init; } = Priority.Medium;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? DueDate { get; init; }
    public int? AssignedUserId { get; init; }
    public IList<string> Tags { get; init; } = [];
}
