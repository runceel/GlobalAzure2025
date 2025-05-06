using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgentsApp.Agents.Activities;
public class WriterActivity(
    [FromKeyedServices("WriterAgent")] Task<Agent> writerAgentTask,
    ILogger<WriterActivity> logger) : 
    AgentActivityBase(writerAgentTask, logger)
{
    [Function(nameof(WriterActivity))]
    public override Task<AgentResponse> RunAsync(
        [ActivityTrigger]
        AgentRequest agentRequest, 
        CancellationToken cancellationToken) => 
        base.RunAsync(agentRequest, cancellationToken);
}
