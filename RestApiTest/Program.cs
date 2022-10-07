namespace RestApiTest;

using Task = Model.Task;

public static class Program
{
    private static readonly List<Task> Tasks = new ();
    
    public static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);    // Фикс для DateTime в PostgreSQL.
        LoadTasksFromDatabase();

        var application = WebApplication.CreateBuilder(args).Build();
        
        application.MapGet("/api/tasks", () => Results.Json(Tasks));
        
        application.MapGet("/api/tasks/{index}", (ulong index) =>
        {
            var task = Tasks.FirstOrDefault(current => current.Id == index);
            
            return task == null
                    ? Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } )
                    : Results.Json(task);
        });
        
        application.MapDelete("/api/tasks/{index}", (ulong index) =>
        {
            var task = Tasks.FirstOrDefault(current => current.Id == index);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } );
            }

            Tasks.Remove(task);
            
            return Results.Json(task);
        });
        
        application.MapPost("/api/tasks", (Task task) =>
        {
            Task? taskFromDb;
            
            using (var db = new ApplicationContext())
            {
                db.Tasks.Add(task);
                db.SaveChanges();
                
                taskFromDb = db.Tasks.ToList().FirstOrDefault(current => current.Name == task.Name);
            }
            
            if (taskFromDb != null)
            {
                Tasks.Add(taskFromDb);
                return Results.Json(taskFromDb);
            }

            return Results.NotFound( new { message = "Не удалось сохранить задачу." } );
        });
        
        application.MapPut("/api/tasks", (Task taskData) =>
        {
            var task = Tasks.FirstOrDefault(current => current.Id == taskData.Id);
            
            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {taskData.Id} не найдена." } );
            }
            
            task.Name = taskData.Name;
            task.StartDate = taskData.StartDate;
            /* Возможно стоит копировать операции. */
            
            return Results.Json(task);
        });
        
        application.Run();
    }

    private static void LoadTasksFromDatabase()
    {
        using var db = new ApplicationContext();
        Tasks.AddRange(db.Tasks.ToList());
    }
}
