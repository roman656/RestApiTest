namespace RestApiTest;

using Model;
using Task = Model.Task;
using System.Globalization;

public static class Program
{
    private const string DateFormat = "dd-MM-yyyy";
    private static readonly List<Task> Tasks = new ()
    {
        new Task
        {
            Index = 1,
            Name = "Разработать БД",
            StartDate = DateTime.ParseExact("28-09-2022", DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None),
            Operations = new List<Operation>
            {
                new Operation() { Index = 1, Name = "Концептуальное моделирование", Duration = 10, Resource = 1 },
                new Operation() { Index = 2, Name = "Развернуть БД", Duration = 1, Resource = 1, PreviousOperations = new List<ulong> { 1 } },
                new Operation() { Index = 3, Name = "Загрузить данные", Duration = 2, Resource = 1 },
                new Operation() { Index = 4, Name = "Разработать приложение", Duration = 10, Resource = 5, PreviousOperations = new List<ulong> { 2, 3 } }
            }
        },
        new Task
        {
            Index = 2,
            Name = "Разработать другую БД",
            StartDate = DateTime.ParseExact("25-10-2022", DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None),
            Operations = new List<Operation>
            {
                new Operation() { Index = 5, Name = "Концептуальное моделирование", Duration = 10, Resource = 1 },
                new Operation() { Index = 6, Name = "Развернуть БД", Duration = 1, Resource = 1, PreviousOperations = new List<ulong> { 5 } },
                new Operation() { Index = 7, Name = "Загрузить данные", Duration = 2, Resource = 1, PreviousOperations = new List<ulong> { 6 } },
                new Operation() { Index = 8, Name = "Разработать приложение", Duration = 10, Resource = 5, PreviousOperations = new List<ulong> { 7 } }
            }
        }
    };
    
    public static void Main(string[] args)
    {
        var application = WebApplication.CreateBuilder(args).Build();
        
        application.MapGet("/api/tasks", () => Results.Json(Tasks));
        
        application.MapGet("/api/tasks/{index}", (ulong index) =>
        {
            var task = Tasks.FirstOrDefault(current => current.Index == index);
            
            return task == null
                    ? Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } )
                    : Results.Json(task);
        });
        
        application.MapDelete("/api/tasks/{index}", (ulong index) =>
        {
            var task = Tasks.FirstOrDefault(current => current.Index == index);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } );
            }

            Tasks.Remove(task);
            
            return Results.Json(task);
        });
        
        application.MapPost("/api/tasks", (Task task) =>
        {
            /* TODO: убедиться в уникальности Index. */
            Tasks.Add(task);    // Возможно стоит обнулять операции.
            
            return Results.Json(task);
        });
        
        application.MapPut("/api/tasks", (Task taskData) =>
        {
            var task = Tasks.FirstOrDefault(current => current.Index == taskData.Index);
            
            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {taskData.Index} не найдена." } );
            }
            
            task.Name = taskData.Name;
            task.StartDate = taskData.StartDate;
            /* Возможно стоит копировать операции. */
            
            return Results.Json(task);
        });
        
        application.Run();
    }
}
