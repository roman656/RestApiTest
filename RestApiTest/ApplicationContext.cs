namespace RestApiTest;

using Microsoft.EntityFrameworkCore;
using Model;
using Task = Model.Task;

public sealed class ApplicationContext : DbContext
{
    private const string Host = "localhost";
    private const string Port = "5432";
    private const string DatabaseName = "rest_api_test";
    private const string Username = "roman";
    private const string Password = "";
    public DbSet<Task> Tasks { get; set; }
    public DbSet<Operation> Operations { get; set; }
         
    public ApplicationContext() => Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql($"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}");
        base.OnConfiguring(optionsBuilder);
    }
}