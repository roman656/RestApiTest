namespace ComputingService;

using Services;
using StackExchange.Redis;

public static class Program
{
    public static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);    // Фикс для DateTime в PostgreSQL.

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrpc();
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost"));

        var application = builder.Build();
        
        application.MapGrpcService<ProjectDurationCalculatorService>();
        application.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        application.Run();
    }
}