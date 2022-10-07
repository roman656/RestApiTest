namespace RestApiTest.Model;

public class Operation
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Duration { get; set; }
    public uint Resource { get; set; }
    public List<ulong> PreviousOperations { get; set; } = new();
}