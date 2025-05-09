using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgentsApp.Agents.Activities;

// Agent を処理する Activity の基本クラス
public abstract class AgentActivityBase(Kernel kernel, Agent agent, ILogger logger)
{
    // Agent を実行する処理
    public virtual async Task<AgentResponse> RunAsync(AgentRequest agentRequest,
        CancellationToken cancellationToken)
    {
        AgentThread? agentThread = null;
        logger.LogInformation("Invoke agent: {AgentName}", agent.Name);
        List<ChatMessageContent> messages = [];
        await foreach (var response in agent.InvokeAsync(
            agentRequest.Messages, agentThread, 
            options: new() { Kernel = kernel, },
            cancellationToken: cancellationToken))
        {
            agentThread = response.Thread;
            if (response.Message is not null)
            {
                messages.Add(response.Message);
            }
        }

        if (agentThread != null)
        {
            await agentThread.DeleteAsync();
        }

        return new(messages);
    }
}
