#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using WriterAndReviewerApp;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

var kernel = builder.Build();

var writerAgent = await AIAgentFactory.CreateWriterAgent(configuration, kernel);
var reviewerAgent = await AIAgentFactory.CreateReviewerAgent(kernel);
AgentGroupChat groupChat = new(writerAgent, reviewerAgent)
{
    ExecutionSettings = new()
    {
        SelectionStrategy = new SequentialSelectionStrategy(),
        TerminationStrategy = new ReviewProcessTerminationStrategy
        {
            Agents = [ reviewerAgent ],
        },
    },
};

const string userInput = ".NET 9 による C# 入門「基本文法編」";

groupChat.AddChatMessage(new(AuthorRole.User, userInput));
while (!groupChat.IsComplete)
{
    await foreach (var response in groupChat.InvokeAsync())
    {
        Console.WriteLine($"{response.AuthorName}: {response.Content}");
        Console.WriteLine();
    }
}

var messages = groupChat.GetChatMessagesAsync();
var answer = await messages.FirstOrDefaultAsync(x => x.AuthorName == writerAgent.Name);

if (answer == null)
{
    Console.WriteLine("No answer found.");
    return;
}

await File.WriteAllTextAsync($"output.md", $"""
    # {userInput}
    
    {answer.Content}
    """);

Process.Start(new ProcessStartInfo("output.md")
{
    UseShellExecute = true,
});
