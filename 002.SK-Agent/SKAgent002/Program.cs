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

// Agent を使用して天気を質問する
const string userInput = "こんにちは！！東京の天気を教えて！";
var result = await agent.InvokeAsync(userInput).FirstAsync();
Console.WriteLine(result.Message.Content);


// WeatherPlugin クラスを定義
class WeatherPlugin
{
    private static readonly string[] WeatherConditions = { "晴れ", "曇り", "雨", "雪", "雷雨" };
    private static readonly Random Random = Random.Shared;

    [KernelFunction]
    [Description("天気予報を取得します。")]
    public string GetWeatherForecast(
        [Description("場所")]
       string location)
    {
        var randomWeather = WeatherConditions[Random.Next(WeatherConditions.Length)];
        return $"{location} の天気は「{randomWeather}」です。";
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
