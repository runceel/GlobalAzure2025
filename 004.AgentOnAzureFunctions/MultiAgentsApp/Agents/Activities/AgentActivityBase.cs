using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgentsApp.Agents.Activities;
public abstract class AgentActivityBase(Task<Agent> agentTask, ILogger logger)
{
    public virtual async Task<AgentResponse> RunAsync(AgentRequest agentRequest,
        CancellationToken cancellationToken)
    {
        var agent = await agentTask;
        AgentThread? agentThread = null;
        logger.LogInformation("Invoke agent: {AgentName}", agent.Name);
        List<ChatMessageContent> messages = [];
        await foreach (var response in agent.InvokeAsync(agentRequest.Messages, agentThread, cancellationToken: cancellationToken))
        {
            agentThread = response.Thread;
            if (response.Message is not null)
            {
                messages.Add(response.Message);
            }
        }

        return new(messages);
    }
}
