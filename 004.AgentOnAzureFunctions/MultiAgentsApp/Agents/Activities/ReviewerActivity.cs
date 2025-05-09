using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgentsApp.Agents.Activities;

// Reviewer agent 用の Activity
public class ReviewerActivity(
    Kernel kernel,
    [FromKeyedServices("ReviewerAgent")] Agent reviewerAgent,
    ILogger<ReviewerActivity> logger) : 
    AgentActivityBase(kernel, reviewerAgent, logger)
{
    [Function(nameof(ReviewerActivity))]
    public override Task<AgentResponse> RunAsync(
        [ActivityTrigger]
        AgentRequest agentRequest, 
        CancellationToken cancellationToken) => 
        base.RunAsync(agentRequest, cancellationToken);
}


