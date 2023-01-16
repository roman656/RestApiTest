namespace RestApiTest.Model;

public class CalculationResult
{
    public bool IsCalculationSuccessful { get; set; }
    public ulong Duration { get; set; }
    public List<string> OperationIndexes { get; set; } = new ();
}