using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now}] Started.");
    await next();
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now}] Finished.");

});

var todos = new List<Todo>();

app.MapGet("/todos", (ITaskService service) => service.GetAllTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
    var task = service.GetTodoById(id);
    return task is null ? TypedResults.NotFound() : TypedResults.Ok(task);
});

app.MapPost("/todos", (Todo task, ITaskService service) =>
{
    var createdTask = service.AddTodo(task);
    return TypedResults.Created($"/todo/{createdTask.Id}", createdTask);
})
.AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if (taskArgument.DueDate < DateTime.UtcNow)
    {
        errors.Add(nameof(Todo.DueDate), ["Due date cannot be in the past."]);
    }
    if (taskArgument.IsCompleted)
    {
        errors.Add(nameof(Todo.IsCompleted), ["New tasks cannot be marked as completed."]);
    }
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }
    return await next(context);
});

app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService
{
    Todo? GetTodoById(int id);

    List<Todo> GetAllTodos();

    void DeleteTodoById(int id);

    Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];

    public Todo? GetTodoById(int id) => _todos.SingleOrDefault(t => t.Id == id);

    public List<Todo> GetAllTodos() => _todos;

    public void DeleteTodoById(int id) => _todos.RemoveAll(t => t.Id == id);

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }
}
