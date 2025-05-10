#pragma warning disable SKEXP0110
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SKAgent002;
using System.ComponentModel;
using System.Globalization;

// 構成情報を読み込む
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
// AOAI のエンドポイントを構成情報から取得
var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

// Semantic Kernel のセットアップ
var builder = Kernel.CreateBuilder();
// Chat Completions API 用のサービスを追加
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

// Human in the loop 用のフィルターを登録
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopFilter>();

// WeatherPlugin クラスをプラグインとして登録
builder.Plugins.AddFromType<WeatherPlugin>();

// Semantic Kernel のセットアップ
var kernel = builder.Build();

// Azure AI Agent Service で Agent を作成
Agent agent = await AIAgentFactory.CreateAgent(configuration, kernel);

// Agent と会話をする
AgentThread? thread = null;
while (true)
{
    Console.Write("User: ");
    string userInput = Console.ReadLine()!;
    if (string.IsNullOrWhiteSpace(userInput))
    {
        break;
    }

    var result = await agent.InvokeAsync(userInput, thread).FirstAsync();
    thread = result.Thread;
    Console.WriteLine($"CatAgent: {result.Message.Content}");
    Console.WriteLine();
}

if (thread != null) await thread.DeleteAsync();


// WeatherPlugin クラスを定義
class WeatherPlugin
{
    [KernelFunction]
    [Description("天気予報を取得します。")]
    public string GetWeatherForecast(
        [Description("天気を知りたい日付")]
        DateTimeOffset date,
        [Description("場所")]
        string location)
    {
        return $"{date} の {location} の天気は「晴れ」です。";
    }

    [KernelFunction]
    [Description("今日の日付を取得します。")]
    public DateTimeOffset GetToday() =>
        TimeProvider.System.GetLocalNow();
}

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
