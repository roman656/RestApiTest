namespace RestApiTest.Model;

public class Task
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public List<Operation> Operations { get; set; } = new ();
}