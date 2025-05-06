#pragma warning disable SKEXP0110
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.ComponentModel;

namespace MultiAgentsApp;
internal static class AIAgentFactory
{
    public static async Task<Microsoft.SemanticKernel.Agents.Agent> CreateWriterAgent(
        IConfiguration configuration, Kernel kernel)
    {
        var projectClient = AzureAIAgent.CreateAzureAIClient(
            configuration.GetConnectionString("AIFoundry")!,
            new AzureCliCredential());
        var bingConnection = await projectClient.GetConnectionsClient()
            .GetConnectionAsync(configuration["BingConnectionName"]!);
        var agentsClient = projectClient.GetAgentsClient();
        var agent = await agentsClient.CreateAgentAsync(
            "gpt-4o",
            name: "WriterAgent",
            description: "ライターエージェント",
            instructions: """
                あなたは記事のライターです。指示されたタイトルの記事を書いてください。
                記事を書く際には最新情報を Bing 検索をして調べつつ書いてください。
                また記事の内容について指摘事項がある場合は、それを踏まえて記事を書き直してください。
                回答には返事や話し言葉は含めずに記事の内容だけを記載してください。
                """,
            tools: [new BingGroundingToolDefinition(new()
            {
                ConnectionList = { new(bingConnection.Value.Id) },
            })]);

        return new AzureAIAgent(agent.Value, agentsClient)
        {
            Kernel = kernel,
        };
    }

    public static Task<Microsoft.SemanticKernel.Agents.Agent> CreateReviewerAgent(Kernel kernel)
    {
        var agent = new ChatCompletionAgent
        {
            Name = "ReviewerAgent",
            Description = "レビュアーエージェント",
            Instructions = """
                あなたは記事のレビュアーです。指示された記事をレビューしてください。
                レビューの結果改善点や追加で必要な情報の指摘をしてください。
                指摘事項は text プロパティに格納してください。
                指摘事項がある場合は finished プロパティを false にしてください。
                指摘事項がない場合は finished プロパティを true にしてください。
                """,
            Kernel = kernel,
            Arguments = new(new AzureOpenAIPromptExecutionSettings
            {
                ResponseFormat = typeof(ReviewResult),
            }),
        };

        return Task.FromResult(agent);
    }
}

public record ReviewResult(
    [Description("レビューの結果")]
    string Text,
    [Description("レビューが完了したかどうか")]
    bool Finished)
{
    public static ReviewResult EmptyFinished { get; } = new("", true);
    public static ReviewResult EmptyNotFinished { get; } = new("", false);
}
