#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

using System.Diagnostics;
using Azure;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using WriterAndReviewerApp.Plugins;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, FunctionInvocationLoggerFilter>();

var kernel = builder.Build();

var writerAgent = await AIAgentFactory.CreateWriterAgent(configuration, kernel);
var reviewerAgent = await AIAgentFactory.CreateReviewerAgent(kernel);
var orchestratorAgent = await AIAgentFactory.CreateOrchestratorAgent(
    kernel,
    writerAgent,
    reviewerAgent);

const string userInput = "ASP.NET Core MVC 入門";
AgentThread? thread = null;
await foreach (var response in orchestratorAgent.InvokeAsync(userInput, thread))
{
    thread = response.Thread;
    Console.WriteLine($"{response.Message.AuthorName}: {response.Message.Content}");
    Console.WriteLine();

    await File.WriteAllTextAsync("output.md", response.Message.Content);
    Process.Start(new ProcessStartInfo
    {
        FileName = "code",
        Arguments = "-r output.md",
        UseShellExecute = true,
    });
}

class FunctionInvocationLoggerFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        Console.WriteLine("=========================================");
        var args = string.Join(", ", context.Arguments?.Select(x => $"{x.Key}: {x.Value}") ?? []);
        Console.WriteLine($"Invoking: {context.Function.PluginName}({args})");
        await next(context);

        var messages = context.Result.GetValue<ChatMessageContent[]>();
        await File.WriteAllTextAsync(
            $"{context.Function.PluginName}.md", 
            messages?.FirstOrDefault()?.Content ?? "");
        Process.Start(new ProcessStartInfo
        {
            FileName = "code",
            Arguments = $"-r {context.Function.PluginName}.md",
            UseShellExecute = true,
        });

    }
}
