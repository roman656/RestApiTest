namespace ComputingService.Services;

using Microsoft.EntityFrameworkCore;
using Model;
using Task = Task;
using Grpc.Core;

public class ProjectDurationCalculatorService : ProjectDurationCalculator.ProjectDurationCalculatorBase
{
    private const int SequencesTotalAmount = 1000000;
    private const int MaxResourcesAmount = 10;
    private static readonly int ThreadsAmount = Environment.ProcessorCount;

    public override Task<CalculationReply> CalculateDuration(CalculationRequest request, ServerCallContext context)
    {
        var reply = new CalculationReply();
        var threads = new Thread[ThreadsAmount];
        var results = new CalculationResult[ThreadsAmount];
        var sequencesAmount = SequencesTotalAmount / ThreadsAmount;
        var operations = GetTaskOperations(request.TaskIndex);

        for (var i = 0; i < ThreadsAmount; i++)
        {
            var index = i;
            
            threads[index] = new Thread(() =>
            {
                results[index] = CalculateSequencesDuration(operations, sequencesAmount);
            });
            threads[index].Start();
        }
        
        for (var j = 0; j < ThreadsAmount; j++)
        {
            threads[j].Join();
        }

        var minDurationResult = FindMinDurationResult(results);
        
        reply.IsCalculationSuccessful = minDurationResult.IsCalculationSuccessful;
        reply.Duration = minDurationResult.Duration;
        reply.OperationIndex.Add(minDurationResult.OperationIndexes);

        return Task.FromResult(reply);
    }

    private static List<Operation> GetTaskOperations(string taskIndex)
    {
        var task = ReadTasksFromDatabase().FirstOrDefault(current => current.Id == taskIndex);

        return task == null ? new List<Operation>() : task.Operations;
    }

    private static List<Model.Task> ReadTasksFromDatabase()
    {
        using var db = new ApplicationContext();
        
        return db.Tasks.Include(task => task.Operations).ToList();
    }

    private static CalculationResult FindMinDurationResult(CalculationResult[] calculationResults)
    {
        var result = calculationResults[0];
        
        foreach (var current in calculationResults)
        {
            if (current.IsCalculationSuccessful && (!result.IsCalculationSuccessful ||
                    (result.IsCalculationSuccessful && current.Duration < result.Duration)))
            {
                result = current;
            }
        }

        return result;
    }

    private static CalculationResult CalculateSequencesDuration(List<Operation> operations, int sequencesAmount)
    {
        var result = new CalculationResult();

        for (var i = 0; i < sequencesAmount; i++)
        {
            result = FindMinDurationResult(new[] { result, CalculateRandomSequence(operations) });
        }

        return result;
    }

    private static CalculationResult CalculateRandomSequence(List<Operation> operations)
    {
        var result = new CalculationResult();
        var random = new Random();
        var waitingOperations = new List<Operation>(operations);    // Список операций, ожидающих выполнения.
        var availableOperations = new List<Operation>();    // Список доступных операций (выполнены все предшествующие).
        var finishedOperations = new List<Operation>();    // Список выполненных операций.
        var processingOperations = new List<Operation>();    // Список операций, находящихся в обработке.
        var duration = 0;

        
        
        return result;
    }
}
