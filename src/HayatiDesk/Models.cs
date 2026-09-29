namespace HayatiDesk;

// Storage shapes match DatabaseContext's existing v1 schema.
public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public string CreatedAt { get; set; } = string.Empty;
}

public sealed class Item
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int Priority { get; set; }
    public string? DueDate { get; set; }
    public bool Completed { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
