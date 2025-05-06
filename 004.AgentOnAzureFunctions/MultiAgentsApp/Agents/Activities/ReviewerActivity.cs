using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgentsApp.Agents.Activities;
public class ReviewerActivity(
    [FromKeyedServices("ReviewerAgent")] Task<Agent> reviewerAgentTask,
    ILogger<ReviewerActivity> logger) : 
    AgentActivityBase(reviewerAgentTask, logger)
{
    [Function(nameof(ReviewerActivity))]
    public override Task<AgentResponse> RunAsync(
        [ActivityTrigger]
        AgentRequest agentRequest, 
        CancellationToken cancellationToken) => 
        base.RunAsync(agentRequest, cancellationToken);
}


