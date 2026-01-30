using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var task = todos.SingleOrDefault(t => t.Id == id);
    return task is null ? TypedResults.NotFound() : TypedResults.Ok(task);
});

app.MapPost("/todos", (Todo task) =>
{
    todos.Add(task);
    return TypedResults.Created($"/todo/{task.Id}", task);
});

app.MapDelete("/todos/{id}", (int id) =>
{
    var task = todos.SingleOrDefault(t => t.Id == id);
    if (task is null)
    {
        return TypedResults.NoContent();
    }
    todos.Remove(task);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);