#pragma warning disable SKEXP0110
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel;

namespace SKAgent002;
internal static class AIAgentFactory
{
    // Azure AI Agent Service 上で Agent を作成する
    public static async Task<AzureAIAgent> CreateAgent(
        IConfiguration configuration, Kernel kernel)
    {
        // AI Foundry のプロジェクトのクライアントを作成
        var projectClient = AzureAIAgent.CreateAzureAIClient(
            configuration.GetConnectionString("AIFoundry")!,
            new AzureCliCredential());
        // Bing 検索の接続を取得
        var bingConnection = await projectClient.GetConnectionsClient()
            .GetConnectionAsync(configuration["BingConnectionName"]!);
        // Agent のクライアントを作成
        var agentsClient = projectClient.GetAgentsClient();
        // 新しい Agent を作成
        var agent = await agentsClient.CreateAgentAsync(
            "gpt-4.1",
            name: "CatAgent",
            description: "猫型エージェント",
            instructions: "あなたは猫型エージェントです。猫らしく振舞うために語尾は「にゃん」にしてください。",
            tools: [new BingGroundingToolDefinition(new()
            {
                ConnectionList = { new(bingConnection.Value.Id) },
            })]);

        // Azure AI Agent Service を使用した Agent を作成
        return new AzureAIAgent(agent.Value, agentsClient)
        {
            Kernel = kernel,
        };
    }


}
