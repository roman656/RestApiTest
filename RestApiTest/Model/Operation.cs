namespace RestApiTest.Model;

using System.Text;

public class Operation
{
    public string Id { get; set; }
    public string Name { get; set; }
    public uint Duration { get; set; }
    public uint Resource { get; set; }
    public List<string> PreviousOperations { get; set; }

    public Operation(string name, uint duration, uint resource)
    {
        Name = name;
        Duration = duration;
        Resource = resource;
        Id = Ulid.NewUlid().ToString();
        PreviousOperations = new List<string>();
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        result.Append("Operation [").Append(Id).Append("]: { Name: ").Append(Name).Append(" | Duration: ");
        result.Append(Duration).Append(" | Resource: ").Append(Resource).Append(" | Previous operations:");

        if (PreviousOperations.Count != 0)
        {
            foreach (var operationId in PreviousOperations)
            {
                result.Append(" ").Append(operationId).Append(",");
            }

            result.Remove(result.Length - 1, 1);    // Удаление последней запятой.
        }
        else
        {
            result.Append(" none");
        }
        
        result.Append(" }");

        return result.ToString();
    }
}