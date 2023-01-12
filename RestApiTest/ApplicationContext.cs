namespace RestApiTest;

using Microsoft.EntityFrameworkCore;
using Model;
using Task = Model.Task;

public sealed class ApplicationContext : DbContext
{
    public DbSet<Task> Tasks { get; set; }
    public DbSet<Operation> Operations { get; set; }

    public ApplicationContext() => Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var builder = new ConfigurationBuilder();

        builder.SetBasePath(Directory.GetCurrentDirectory());
        builder.AddJsonFile("appsettings.json");

        var config = builder.Build();
        
        optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));
        base.OnConfiguring(optionsBuilder);
    }
}