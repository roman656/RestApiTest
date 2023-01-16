namespace ComputingService.Services;

using Grpc.Core;

public class ProjectDurationCalculatorService : ProjectDurationCalculator.ProjectDurationCalculatorBase
{
    private readonly ILogger<ProjectDurationCalculatorService> _logger;
    
    public ProjectDurationCalculatorService(ILogger<ProjectDurationCalculatorService> logger) => _logger = logger;

    public override Task<CalculationReply> CalculateDuration(CalculationRequest request, ServerCallContext context)
    {
        var reply = new CalculationReply();
        
        reply.IsCalculationSuccessful = true;
        reply.Duration = 0;
        reply.OperationIndex.Add(request.TaskIndex);
        reply.OperationIndex.Add(request.TaskIndex);
        reply.OperationIndex.Add(request.TaskIndex);
        
        return Task.FromResult(reply);
    }
}
