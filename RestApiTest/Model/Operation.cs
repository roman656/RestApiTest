namespace RestApiTest.Model;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text;

[Table("operations")]
public class Operation
{
    [Key, Column("id")]
    public string Id { get; set; }

    [Column("name")]
    public string Name { get; set; }
    
    [Column("duration")]
    public uint Duration { get; set; }
    
    [Column("resource")]
    public uint Resource { get; set; }
    
    [Column("previous_operations")]
    public List<string> PreviousOperations { get; set; }
    
    public string TaskId { get; set; }

    public Operation(string name, uint duration, uint resource, string taskId)
    {
        Name = name;
        Duration = duration;
        Resource = resource;
        TaskId = taskId;
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
                result.Append(' ').Append(operationId).Append(',');
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