using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.IncludeFields = true;
});
builder.Services.AddControllers();

var app = builder.Build();

var todoItems = app.MapGroup("/todoitems");

todoItems.MapGet("/", GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id}", GetTodo);
todoItems.MapPost("/", CreateTodos);
todoItems.MapPut("/{id}", UpdateTodo);
todoItems.MapDelete("/{id}", DeleteTodo);

app.Run();

static async Task<IResult> GetAllTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToListAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(new TodoItemDTO(todo))
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodos(List<TodoItemDTO> todoItemDTOs, TodoDb db)
{
    var todoItems = todoItemDTOs.Select (dto => new Todo
    {
        IsComplete = dto.IsComplete,
        Name = dto.Name
    }).ToList();

    db.Todos.AddRange(todoItems);
    await db.SaveChangesAsync();

    var createdTodoDTOs = todoItems.Select(t => new TodoItemDTO(t)).ToList();
    return TypedResults.Created("/todoitems/", createdTodoDTOs);
}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDb db)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;

    var newTodo = new Todo
    {
        Name = todoItemDTO.Name + " - New",
        IsComplete = todoItemDTO.IsComplete,
    };

    db.Todos.Add(newTodo);
    await db.SaveChangesAsync();

    var response = new
    {
        Updated = new TodoItemDTO(todo),
        Created = new TodoItemDTO(newTodo)
    };

    return TypedResults.Ok(response);
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    { 
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    return TypedResults.NotFound();
}

//todoItems.MapGet("/", async (TodoDb db) =>
//    await db.Todos.ToListAsync());

//todoItems.MapGet("/complete", async (TodoDb db) =>
//    await db.Todos.Where( t => t.IsComplete).ToListAsync());

//todoItems.MapGet("/{id}", async (int id, TodoDb db) =>
//    await db.Todos.FindAsync(id)
//        is Todo todo
//            ? Results.Ok(todo)
//            : Results.NotFound());

//todoItems.MapPost("/", async (Todo todo, TodoDb db) =>
//{
//    db.Todos.Add(todo);
//    await db.SaveChangesAsync();

//    return Results.Created($"/todoitems/{todo.Id}", todo);
//});

//todoItems.MapPut("/{id}", async (int id, Todo inputTodo, TodoDb db) =>
//{
//    var todo = await db.Todos.FindAsync(id);

//    if (todo is null) return Results.NotFound();

//    todo.Name = inputTodo.Name;
//    todo.IsComplete = inputTodo.IsComplete;

//    await db.SaveChangesAsync();

//    return Results.NoContent();
//});

//todoItems.MapDelete("/{id}", async (int id, TodoDb db) =>
//{
//    if (await db.Todos.FindAsync(id) is Todo todo)
//    {
//        db.Todos.Remove(todo);
//        await db.SaveChangesAsync();
//        return Results.NoContent();
//    }

//    return Results.NotFound();
//});

//app.Run();