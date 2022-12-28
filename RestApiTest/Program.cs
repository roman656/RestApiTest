using Microsoft.EntityFrameworkCore;

namespace RestApiTest;

using Task = Model.Task;

public static class Program
{
    public static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);    // Фикс для DateTime в PostgreSQL.

        var application = WebApplication.CreateBuilder(args).Build();  
        
        application.MapGet("/api/tasks", () => Results.Json(LoadTasksFromDatabase())); 
        
        
        application.MapGet("/api/tasks/{index}", (string index) =>
        {
            var task = LoadTasksFromDatabase().FirstOrDefault(current => current.Id == index);
            
            return task == null
                    ? Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } )
                    : Results.Json(task);
        });
        
        
        application.MapDelete("/api/tasks/{index}", (string index) =>
        {
            var task = LoadTasksFromDatabase().FirstOrDefault(current => current.Id == index);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } );
            }
            
            DeleteTaskFromDatabase(task);
            
            return Results.Json(task);
        });
        
        
        application.MapPost("/api/tasks", (Task task) =>
        {
            SaveTasksToDatabase(new List<Task> { task });
            
            return Results.Json(task);
        });
        
        
        application.MapPut("/api/tasks/{index}", (string index, Task taskData) =>
        {
            taskData.Id = index;
            var task = LoadTasksFromDatabase().FirstOrDefault(current => current.Id == taskData.Id);
           
            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {taskData.Id} не найдена." } );
            }
            
            UpdateTaskInDatabase(taskData);

            return Results.Json(taskData);
        });
        
        application.Run();
    }

    
    private static List<Task> LoadTasksFromDatabase()
    {
        using var db = new ApplicationContext();

        var operations = db.Operations.ToList();    // Магическим образом в Tasks подтягиваются операции.
        
        return db.Tasks.ToList();
    }
    
    
    private static void SaveTasksToDatabase(List<Task> tasks)
    {
        using var db = new ApplicationContext();
        
        db.Tasks.AddRange(tasks);
        db.SaveChanges();
    }

    
    /// TODO: каскадно удалять связанные операции.
    private static void DeleteTaskFromDatabase(Task task)
    {
        using var db = new ApplicationContext();

        db.Tasks.Where(current => current.Id == task.Id).ExecuteDelete();
        db.SaveChanges();
    }
    
    
    private static void UpdateTaskInDatabase(Task task)
    {
        using var db = new ApplicationContext();

        db.Tasks.Update(task);
        db.SaveChanges();
    }
}
