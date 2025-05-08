
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

// Human in the loop 用のフィルターを登録
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopFilter>();

builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();

var result = await kernel.InvokePromptAsync("""
    <message role="system">
        あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
    </message>
    <message role="user">
        こんにちは、私の名前は {{$name}} です。
        {{$location}} の天気を教えてください。
    </message>
    """,
    arguments: new(new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        })
    {
        ["name"] = "セマンティックカーネル",
        ["location"] = "東京",
    });

Console.WriteLine(result.GetValue<string>());


class WeatherPlugin
{
    [KernelFunction]
    [Description("天気予報を取得します。")]
    public string GetWeatherForecast(
        [Description("場所")]
        string location)
    {
        return $"{location} の天気は「晴れ」です。";
    }
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
