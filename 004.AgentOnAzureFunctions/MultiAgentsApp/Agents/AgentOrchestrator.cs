using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MultiAgentsApp.Agents.Activities;

namespace MultiAgentsApp.Agents;
public class AgentOrchestrator
{
    public static TaskOptions s_options = new(retry: new(new RetryPolicy(5, TimeSpan.FromSeconds(1))));

    [Function(nameof(AgentOrchestrator))]
    public async Task<AgentResponse> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // 入力を受け取る
        var agentRequest = context.GetInput<AgentRequest>();
        ArgumentNullException.ThrowIfNull(agentRequest);

        ChatHistory chatHistory = [.. agentRequest.Messages];
        ChatMessageContent? latestWriterMessage = null;
        var reviewResult = ReviewResult.EmptyNotFinished;
        context.SetCustomStatus("ライターが執筆中です。");
        int loopCount = 1;
        // レビューが終わるまでループ
        while (reviewResult.Finished is false || loopCount <= 10)
        {
            // ライターに執筆を依頼
            var writerAgentResponse = await context.CallActivityAsync<AgentResponse>(
                nameof(WriterActivity),
                new AgentRequest(chatHistory),
                s_options);
            chatHistory.AddRange(writerAgentResponse.Messages);
            latestWriterMessage = writerAgentResponse.Messages.Last();

            context.SetCustomStatus($"""

                レビューワーがレビュー中です。({loopCount} 回目)
                -------
                ## 現在の文章
                {latestWriterMessage.Content}
                """);

            // レビューワーにレビューを依頼
            var reviewerAgentResponse = await context.CallActivityAsync<AgentResponse>(
                nameof(ReviewerActivity),
                new AgentRequest(chatHistory),
                s_options);
            var reviewerMessage = reviewerAgentResponse.Messages.Last();
            reviewResult = 
                JsonSerializer.Deserialize<ReviewResult>(reviewerMessage.Content ?? "{}") 
                ?? ReviewResult.EmptyFinished;
            chatHistory.AddRange(reviewerAgentResponse.Messages);

            if (reviewResult.Finished)
            {
                context.SetCustomStatus("ライターが執筆を終了しました。");
            }
            else
            {
                context.SetCustomStatus($"""
                    以下の指摘事項を反映中です。({loopCount} 回目)
                    {reviewResult.Text}
                    ----------
                    ## 現在の文章
                    {latestWriterMessage.Content}
                    """);
                    
            }

            loopCount++;
        }

        if (latestWriterMessage is null)
        {
            throw new InvalidOperationException("Writer message is null");
        }

        // レビューが終わったら、ライターのメッセージを返す
        return new([latestWriterMessage]);
    }
}
