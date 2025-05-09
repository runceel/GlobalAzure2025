
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
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

// 東京の天気を質問する
var result = await kernel.InvokePromptAsync("""
    <message role="system">
        あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
    </message>
    <message role="user">
        こんにちは、私の名前はかずきです。
        東京の天気を教えてください。
    </message>
    """,
    arguments: new(new PromptExecutionSettings
    {
        // プラグインの関数を自動で呼び出すように設定
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    }));

// 結果を取得
Console.WriteLine(result.GetValue<string>());

// WeatherPlugin クラスを定義
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
