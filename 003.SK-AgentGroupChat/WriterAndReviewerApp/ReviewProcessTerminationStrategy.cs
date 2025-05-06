#pragma warning disable SKEXP0110
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using System.Text.Json;

namespace WriterAndReviewerApp;
internal class ReviewProcessTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        var reviewResult = JsonSerializer.Deserialize<ReviewResult>(
            history[^1].Content ?? "{}");
        if (reviewResult is null) return Task.FromResult(false);

        return Task.FromResult(reviewResult.Finished);
    }
}
