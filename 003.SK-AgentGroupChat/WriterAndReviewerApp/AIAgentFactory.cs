#pragma warning disable SKEXP0110
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.ComponentModel;

namespace WriterAndReviewerApp;
internal static class AIAgentFactory
{
    public static async Task<AzureAIAgent> CreateWriterAgent(
        IConfiguration configuration, Kernel kernel)
    {
        var projectClient = AzureAIAgent.CreateAzureAIClient(
            configuration.GetConnectionString("AIFoundry")!,
            new AzureCliCredential());
        var bingConnection = await projectClient.GetConnectionsClient()
            .GetConnectionAsync(configuration["BingConnectionName"]!);
        var agentsClient = projectClient.GetAgentsClient();
        var agent = await agentsClient.CreateAgentAsync(
            "gpt-4.1",
            name: "WriterAgent",
            description: "ライターエージェント",
            instructions: """
                あなたは記事のライターです。指示されたタイトルの記事を markdown 形式で書いてください。
                記事を書く際には最新情報を Bing 検索をして調べつつ書いてください。
                また記事の内容について指摘事項がある場合は、それを踏まえて記事を書き直してください。
                回答には返事や話し言葉は含めずに記事の内容だけを記載してください。
                図を使用した方がわかりやすい場合には mermaid 記法を使用してください。
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

    public static Task<ChatCompletionAgent> CreateReviewerAgent(Kernel kernel)
    {
        var agent = new ChatCompletionAgent
        {
            Name = "ReviewerAgent",
            Description = "レビュアーエージェント",
            Instructions = """
                あなたは記事のレビュアーです。指示された記事をレビューしてください。
                レビューの結果改善点や追加で必要な情報の指摘をしてください。レビューの前提条件は以下の通りです。
                1. 記事は日本語で書かれていること
                2. 画像は使用できないため、画像に関する指摘はしないこと
                3. 記事の内容は正確であること
                4. 過不足なく必要な情報が含まれていること

                指摘事項がある場合は finished プロパティを false にしてください。
                指摘事項がない場合は finished プロパティを true にしてください。
                
                指摘事項は text プロパティに以下の書式で格納してください。
                指摘事項には修正が必要な点のみを記載して挨拶などの余計な情報は含めないでください。
                
                記事に関する指摘事項:
                - 指摘事項1
                - 指摘事項2
                - 以下指摘事項の数だけ箇条書きで指摘してください。
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
    bool Finished);
