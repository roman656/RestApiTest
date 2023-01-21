namespace ComputingService.Services;

using System.Text.Json;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using Model;
using Task = Task;
using Grpc.Core;

public class ProjectDurationCalculatorService : ProjectDurationCalculator.ProjectDurationCalculatorBase
{
    private const int SequencesTotalAmount = 1000000;
    private const ulong MaxResourcesAmount = 10;
    private const int ExpireTimeInSeconds = 15;
    private static readonly int ThreadsAmount = Environment.ProcessorCount;
    private readonly IDatabase _redis;
    
    public ProjectDurationCalculatorService(IConnectionMultiplexer muxer)
    {
        _redis = muxer.GetDatabase();
    }

    private async Task<CalculationResult> CheckCalculationResultInRedis(string taskIndex)
    {
        var json = await _redis.StringGetAsync(taskIndex);

        return string.IsNullOrEmpty(json)
                ? new CalculationResult()
                : JsonSerializer.Deserialize<CalculationResult>(json);
    }
    
    private async void SaveCalculationResultInRedis(string taskIndex, CalculationResult calculationResult)
    {
        var json = JsonSerializer.Serialize(calculationResult);
        
        if (!string.IsNullOrEmpty(json))
        {
            var setTask = _redis.StringSetAsync(taskIndex, json);
            var expireTask = _redis.KeyExpireAsync(taskIndex, TimeSpan.FromSeconds(ExpireTimeInSeconds));
            
            await Task.WhenAll(setTask, expireTask);
        }
    }

    public override Task<CalculationReply> CalculateDuration(CalculationRequest request, ServerCallContext context)
    {
        var reply = new CalculationReply();
        var resultFromRedis = CheckCalculationResultInRedis(request.TaskIndex).Result;

        if (resultFromRedis.IsCalculationSuccessful)
        {
            reply.IsCalculationSuccessful = resultFromRedis.IsCalculationSuccessful;
            reply.Duration = resultFromRedis.Duration;
            reply.OperationIndex.Add(resultFromRedis.OperationIndexes);

            return Task.FromResult(reply);
        }
        
        var threads = new Thread[ThreadsAmount];
        var calculationResults = new CalculationResult[ThreadsAmount];
        var sequencesAmount = SequencesTotalAmount / ThreadsAmount;
        var operations = GetTaskOperations(request.TaskIndex);

        if (!operations.Any())
        {
            reply.IsCalculationSuccessful = true;
            
            return Task.FromResult(reply);
        }

        for (var i = 0; i < ThreadsAmount; i++)
        {
            var index = i;
            
            threads[index] = new Thread(() =>
            {
                calculationResults[index] = CalculateMinimalSequenceDuration(operations, sequencesAmount);
            });
            threads[index].Start();
        }
        
        for (var j = 0; j < ThreadsAmount; j++)
        {
            threads[j].Join();
        }

        var result = FindMinimalDurationResult(calculationResults);
        
        reply.IsCalculationSuccessful = result.IsCalculationSuccessful;
        reply.Duration = result.Duration;
        reply.OperationIndex.Add(result.OperationIndexes);
        
        SaveCalculationResultInRedis(request.TaskIndex, result);
        
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

    /// Предпочтение отдаётся валидному результату.
    private static CalculationResult FindMinimalDurationResult(in CalculationResult[] calculationResults)
    {
        var result = calculationResults[0];

        for (var index = 0; index < calculationResults.Length; index++)
        {
            if (calculationResults[index].IsCalculationSuccessful)
            {
                var hasNoValidResult = !result.IsCalculationSuccessful;
                
                if (hasNoValidResult || calculationResults[index].Duration < result.Duration)
                {
                    result = calculationResults[index];
                }
            }
        }

        return result;
    }
    
    /// Предпочтение отдаётся валидному результату.
    private static CalculationResult FindMinimalDurationResult(in CalculationResult first, in CalculationResult second)
    {
        if (second.IsCalculationSuccessful && (!first.IsCalculationSuccessful || second.Duration < first.Duration))
        {
            return second;
        }

        return first;
    }

    private static CalculationResult CalculateMinimalSequenceDuration(in List<Operation> operations, int sequencesAmount)
    {
        var result = new CalculationResult();
        var random = new Random();
        var waitingOperations = new List<Operation>();    // Список операций, ожидающих выполнения.
        var availableOperations = new List<Operation>();    // Список доступных операций (выполнены все предшествующие).
        var processingOperations = new List<Operation>();    // Список операций, находящихся в обработке.
        var finishedOperations = new List<Operation>();    // Список выполненных операций.
        var finishedOperationIndexes = new List<string>();

        for (var i = 0; i < sequencesAmount; i++)
        {
            waitingOperations.Clear();
            availableOperations.Clear();
            finishedOperations.Clear();
            processingOperations.Clear();
            finishedOperationIndexes.Clear();
            waitingOperations.AddRange(operations.Select(operation => (Operation)operation.Clone()));

            result = FindMinimalDurationResult(result, CalculateRandomSequenceDuration(waitingOperations,
                    availableOperations, finishedOperations, processingOperations, finishedOperationIndexes, random));

            if (!result.IsCalculationSuccessful)    // Некорректные входные данные.
            {
                break;
            }
        }

        return result;
    }

    private static CalculationResult CalculateRandomSequenceDuration(in List<Operation> waitingOperations,
            in List<Operation> availableOperations, in List<Operation> finishedOperations,
            in List<Operation> processingOperations, in List<string> finishedOperationIndexes, in Random random)
    {
        var duration = 0UL;
        uint minimalDuration;

        while (waitingOperations.Any() || processingOperations.Any())
        {
            UpdateProcessingOperations(processingOperations, waitingOperations, availableOperations,
                    finishedOperationIndexes, random);

            if (!processingOperations.Any() && waitingOperations.Any())    // Некорректные входные данные.
            {
                return new CalculationResult();
            }

            minimalDuration = FindMinimalDuration(processingOperations);
            ProcessOperations(minimalDuration, processingOperations, finishedOperations, finishedOperationIndexes);
            RemoveProcessedOperations(processingOperations);
            duration += minimalDuration;
        }

        return PrepareCalculationResult(duration, finishedOperations);
    }

    private static void UpdateProcessingOperations(in List<Operation> processingOperations,
            in List<Operation> waitingOperations, in List<Operation> availableOperations,
            in List<string> finishedOperationIndexes, in Random random)
    {
        var wasFoundOperationToProcess = true;
        var involvedResourcesAmount = 0UL;
            
        while (wasFoundOperationToProcess)
        {
            wasFoundOperationToProcess = false;
            involvedResourcesAmount = GetInvolvedResourcesAmount(processingOperations);
            UpdateAvailableOperations(availableOperations, waitingOperations, finishedOperationIndexes, involvedResourcesAmount);

            if (availableOperations.Any())
            {
                var operationToProcess = availableOperations[random.Next(0, availableOperations.Count)];

                processingOperations.Add(operationToProcess);
                availableOperations.Remove(operationToProcess);
                waitingOperations.Remove(operationToProcess);
                wasFoundOperationToProcess = true;
            }
        }
    }

    private static CalculationResult PrepareCalculationResult(ulong duration, in List<Operation> finishedOperations)
    {
        var result = new CalculationResult
        {
            IsCalculationSuccessful = true,
            Duration = duration
        };
        
        foreach (var operation in finishedOperations)
        {
            result.OperationIndexes.Add(operation.Id);
        }

        return result;
    }

    private static void ProcessOperations(uint processTime, in List<Operation> processingOperations,
            in List<Operation> finishedOperations, in List<string> finishedOperationIndexes)
    {
        for (var i = 0; i < processingOperations.Count; i++)
        {
            processingOperations[i].Duration -= processTime;

            if (processingOperations[i].Duration <= 0U)
            {
                finishedOperations.Add(processingOperations[i]);
                finishedOperationIndexes.Add(processingOperations[i].Id);
            }
        }
    }

    private static void RemoveProcessedOperations(in List<Operation> processingOperations)
    {
        processingOperations.RemoveAll(operation => operation.Duration <= 0U);
    }

    private static uint FindMinimalDuration(in List<Operation> processingOperations)
    {
        var result = 0U;

        for (var i = 0; i < processingOperations.Count; i++)
        {
            if (result > 0U)
            {
                result = result > processingOperations[i].Duration ? processingOperations[i].Duration : result;
            }
            else
            {
                result = processingOperations[i].Duration;
            }
        }

        return result;
    }

    private static void UpdateAvailableOperations(in List<Operation> availableOperations, in List<Operation> waitingOperations,
            in List<string> finishedOperationIndexes, ulong involvedResourcesAmount)
    {
        for (var i = 0; i < waitingOperations.Count; i++)
        {
            if (availableOperations.Contains(waitingOperations[i]) ||
                    waitingOperations[i].Resource + involvedResourcesAmount > MaxResourcesAmount)
            {
                continue;
            }
            
            var isAvailable = true;

            for (var j = 0; j < waitingOperations[i].PreviousOperations.Count; j++)
            {
                var previousOperationIndex = waitingOperations[i].PreviousOperations[j];
                
                if (!finishedOperationIndexes.Contains(previousOperationIndex))
                {
                    isAvailable = false;
                    break;
                }
            }

            if (isAvailable)
            {
                availableOperations.Add(waitingOperations[i]);
            }
        }
    }

    private static ulong GetInvolvedResourcesAmount(in List<Operation> processingOperations)
    {
        var result = 0UL;

        for (var index = 0; index < processingOperations.Count; index++)
        {
            result += processingOperations[index].Resource;
        }

        return result;
    }
}
