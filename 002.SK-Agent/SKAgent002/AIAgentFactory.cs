#pragma warning disable SKEXP0110
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel;

namespace SKAgent002;
internal static class AIAgentFactory
{
    public static async Task<AzureAIAgent> CreateAgent(
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
            name: "CatAgent",
            description: "猫型エージェント",
            instructions: "あなたは猫型エージェントです。猫らしく振舞うために語尾は「にゃん」にしてください。",
            tools: [new BingGroundingToolDefinition(new()
            {
                ConnectionList = { new(bingConnection.Value.Id) },
            })]);

        return new AzureAIAgent(agent.Value, agentsClient)
        {
            Kernel = kernel,
        };
    }


}
