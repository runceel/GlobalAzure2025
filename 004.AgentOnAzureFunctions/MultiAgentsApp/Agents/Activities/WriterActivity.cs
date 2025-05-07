using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgentsApp.Agents.Activities;
public class WriterActivity(
    Kernel kernel,
    [FromKeyedServices("WriterAgent")] Agent writerAgent,
    ILogger<WriterActivity> logger) : 
    AgentActivityBase(kernel, writerAgent, logger)
{
    [Function(nameof(WriterActivity))]
    public override Task<AgentResponse> RunAsync(
        [ActivityTrigger]
        AgentRequest agentRequest, 
        CancellationToken cancellationToken) => 
        base.RunAsync(agentRequest, cancellationToken);
}
