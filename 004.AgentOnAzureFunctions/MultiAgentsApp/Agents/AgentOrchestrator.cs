using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MultiAgentsApp.Agents.Activities;

namespace MultiAgentsApp.Agents;
public class AgentOrchestrator
{
    [Function(nameof(AgentOrchestrator))]
    public async Task<AgentResponse> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var agentRequest = context.GetInput<AgentRequest>();
        ArgumentNullException.ThrowIfNull(agentRequest);

        ChatHistory chatHistory = [.. agentRequest.Messages];
        ChatMessageContent? latestWriterMessage = null;
        var reviewResult = ReviewResult.EmptyNotFinished;
        while(reviewResult.Finished is false)
        {
            var writerAgentResponse = await context.CallActivityAsync<AgentResponse>(
                nameof(WriterActivity),
                new AgentRequest(chatHistory));
            chatHistory.AddRange(writerAgentResponse.Messages);
            latestWriterMessage = writerAgentResponse.Messages.Last();

            var reviewerAgentResponse = await context.CallActivityAsync<AgentResponse>(
                nameof(ReviewerActivity),
                new AgentRequest(chatHistory));
            var reviewerMessage = reviewerAgentResponse.Messages.Last();
            reviewResult = 
                JsonSerializer.Deserialize<ReviewResult>(reviewerMessage.Content ?? "{}") 
                ?? ReviewResult.EmptyFinished;
            chatHistory.AddRange(reviewerAgentResponse.Messages);
        }

        if (latestWriterMessage is null)
        {
            throw new InvalidOperationException("Writer message is null");
        }

        return new([latestWriterMessage]);
    }
}
