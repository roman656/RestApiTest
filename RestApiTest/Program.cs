namespace RestApiTest;

using Model;
using Microsoft.EntityFrameworkCore;
using Task = Model.Task;
using Grpc.Net.Client;

public static class Program
{
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);    // Фикс для DateTime в PostgreSQL.
        
        var application = WebApplication.CreateBuilder(args).Build();
        

        using var channel = GrpcChannel.ForAddress("http://localhost:5271");
        var client = new ProjectDurationCalculator.ProjectDurationCalculatorClient(channel);
        var reply = await client.CalculateDurationAsync(new CalculationRequest { TaskIndex = "name3" });
        Console.WriteLine($"Ответ сервера: {reply.OperationIndex}");

        AddEndpointHandlers(application);
        application.Run();
    }

    private static void AddEndpointHandlers(WebApplication application)
    {
        application.MapGet("/api/tasks", () => Results.Json(ReadTasksFromDatabase()));
        application.MapGet("/api/tasks/{index}", (string index) =>
        {
            var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == index);
            
            return task == null
                    ? Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } )
                    : Results.Json(task);
        });
        application.MapDelete("/api/tasks/{index}", (string index) =>
        {
            var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == index);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } );
            }

            DeleteTaskFromDatabase(task);
            
            return Results.Json(task);
        });
        application.MapPost("/api/tasks", (Task task) =>
        {
            task.Operations.Clear();
            SaveTaskToDatabase(task);
            
            return Results.Json(task);
        });
        application.MapPut("/api/tasks/{index}", (string index, Task taskData) =>
        {
            var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == index);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {index} не найдена." } );
            }
            
            taskData.Id = index;
            taskData.Operations = task.Operations;    // Операции обновляются отдельным способом.
            UpdateTaskInDatabase(taskData);

            return Results.Json(taskData);
        });
        application.MapPost("/api/tasks/{taskIndex}/operations", (string taskIndex, Operation operation) =>
        {
            var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == taskIndex);
           
            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {taskIndex} не найдена." } );
            }
            
            operation.TaskId = taskIndex;
            operation.PreviousOperations.Clear();
            task.Operations.Add(operation);
            SaveOperationToDatabase(operation);

            return Results.Json(task);
        });
        application.MapDelete("/api/tasks/{taskIndex}/operations/{operationIndex}",
                (string taskIndex, string operationIndex) =>
        {
            var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == taskIndex);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {taskIndex} не найдена." } );
            }

            var operation = task.Operations.FirstOrDefault(current => current.Id == operationIndex);
            
            if (operation == null)
            {
                return Results.NotFound( new { message = $"Операция под индексом {operationIndex} не найдена." } );
            }
            
            DeleteOperationFromDatabase(operation);
            task.Operations.Remove(operation);

            return Results.Json(task);
        });
        application.MapPut("/api/tasks/{taskIndex}/operations/{operationIndex}",
                (string taskIndex, string operationIndex, Operation prevOperationsData) =>
        {
            var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == taskIndex);

            if (task == null)
            {
                return Results.NotFound( new { message = $"Задача под индексом {taskIndex} не найдена." } );
            }

            var operation = task.Operations.FirstOrDefault(current => current.Id == operationIndex);
            
            if (operation == null)
            {
                return Results.NotFound( new { message = $"Операция под индексом {operationIndex} не найдена." } );
            }

            operation.PreviousOperations = prevOperationsData.PreviousOperations;
            UpdateOperationInDatabase(operation);

            return Results.Json(task);
        });
    }

    private static void SaveTaskToDatabase(Task task)
    {
        using var db = new ApplicationContext();
        
        db.Tasks.Add(task);
        db.SaveChanges();
    }
    
    private static void SaveOperationToDatabase(Operation operation)
    {
        using var db = new ApplicationContext();
        
        db.Operations.Add(operation);
        db.SaveChanges();
    }
    
    private static List<Task> ReadTasksFromDatabase()
    {
        using var db = new ApplicationContext();
        
        return db.Tasks.Include(task => task.Operations).ToList();
    }

    private static void UpdateTaskInDatabase(Task task)
    {
        using var db = new ApplicationContext();

        db.Tasks.Update(task);
        db.SaveChanges();
    }
    
    private static void UpdateOperationInDatabase(Operation operation)
    {
        using var db = new ApplicationContext();

        db.Operations.Update(operation);
        db.SaveChanges();
    }
    
    private static void DeleteTaskFromDatabase(Task task)
    {
        using var db = new ApplicationContext();

        db.Tasks.Remove(task);
        db.SaveChanges();
    }
    
    private static void DeleteOperationFromDatabase(Operation operation)
    {
        using var db = new ApplicationContext();

        db.Operations.Remove(operation);
        db.SaveChanges();
    }
}
