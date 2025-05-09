#pragma warning disable SKEXP0110
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using System.Text.Json;

namespace WriterAndReviewerApp;

// レビューが完了したときに終了するための Strategy
internal class ReviewProcessTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent, 
        IReadOnlyList<ChatMessageContent> history, 
        CancellationToken cancellationToken)
    {
        // 最後のメッセージがレビュー結果であることを確認
        var reviewResult = JsonSerializer.Deserialize<ReviewResult>(
            history[^1].Content ?? "{}");
        if (reviewResult is null) return Task.FromResult(false);

        // レビュー結果の finished プロパティが true の場合は終了
        return Task.FromResult(reviewResult.Finished);
    }
}
