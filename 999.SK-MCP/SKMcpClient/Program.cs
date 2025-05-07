#pragma warning disable SKEXP0001
using System.Globalization;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

// 構成を読み込む
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// MCP クライアントを作成
var mcpServerExePath = configuration["McpServerPath"]
    ?? throw new InvalidOperationException("McpServerPath is not set in the configuration.");

await using var client = await McpClientFactory.CreateAsync(
    new StdioClientTransport(options: new()
    {
        Command = mcpServerExePath,
    }));

// MCP のツールを取得と表示
IList<McpClientTool> tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}
Console.WriteLine();

// Semantic Kernel のセットアップ
var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopFilter>();

var kernel = builder.Build();
// MCP のツールをプラグインとして登録
kernel.Plugins.AddFromFunctions(
    "McpTools",
    tools.Select(x => x.AsKernelFunction()));

// 猫型エージェントを作成
var catAgent = new ChatCompletionAgent
{
    Name = "CatAgent",
    Instructions = """
        あなたは猫型エージェントです。猫らしく振舞うために語尾は「にゃん」にしてください。
        ツールを駆使してユーザーの要望にこたえてください。
        わからないことについては、無理に答えずにわからないと答えてください。
        """,
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings
    {
        // 関数を自動で呼び出す
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    })
};

// 今日の品川の天気を確認
const string userInput = "今日の品川の天気を教えて。";
var result = await catAgent.InvokeAsync(userInput).FirstAsync();
Console.WriteLine($"{result.Message.AuthorName}: {result.Message.Content}");

// Human in the loop 用のフィルター
class HumanInTheLoopFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        var args = string.Join(
            ", ", 
            context.Arguments?.Select(x => $"{x.Key}: {x.Value}") ?? []);
        Console.WriteLine($"{context.Function.Name}({args}) を呼んでもいいですか？(y/n)");
        var answer = Console.ReadLine() ?? "n";
        if (answer.ToLower(CultureInfo.InvariantCulture) == "y")
        {
            await next(context);
        }
        else
        {
            context.Result = new(context.Result, "ユーザーがキャンセルしました。");
            context.Terminate = true;
        }
    }
}
