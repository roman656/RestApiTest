namespace RestApiTest;

using Microsoft.EntityFrameworkCore;
using Task = RestApiTest.Model.Task;

public sealed class ApplicationContext : DbContext
{
    public DbSet<Task> Tasks { get; set; }
         
    public ApplicationContext()
    {
        Database.EnsureCreated();
    }
 
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=rest_api_test;Username=roman;Password=");
    }
}