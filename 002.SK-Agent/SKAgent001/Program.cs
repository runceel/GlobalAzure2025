using System.ComponentModel;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

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
//builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopFilter>();
// WeatherPlugin クラスをプラグインとして登録
//builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();

Agent agent = new ChatCompletionAgent
{
    // Agent 名
    Name = "CatAgent",
    // Agent への指示 (System Prompt)
    Instructions = """
        あなたは猫型アシスタンスです。猫らしく振舞うために語尾は「にゃん」にしてください。
        わからないことに関しては素直にわからないという旨を猫っぽく伝えてください。
        """,
    // Agent が使用するプラグインなどを含んだ Kernel
    Kernel = kernel,
    // Agent の細かい設定
    //Arguments = new(new PromptExecutionSettings()
    //{
    //    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    //}),
};

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
//class WeatherPlugin
//{
//    [KernelFunction]
//    [Description("天気予報を取得します。")]
//    public string GetWeatherForecast(
//        [Description("天気を知りたい日付")]
//        DateTimeOffset date,
//        [Description("場所")]
//        string location)
//    {
//        return $"{date} の {location} の天気は「晴れ」です。";
//    }

//    [KernelFunction]
//    [Description("今日の日付を取得します。")]
//    public DateTimeOffset GetToday() =>
//        TimeProvider.System.GetLocalNow();
//}

// Human in the loop 用のフィルター
//class HumanInTheLoopFilter : IAutoFunctionInvocationFilter
//{
//    public async Task OnAutoFunctionInvocationAsync(
//        AutoFunctionInvocationContext context,
//        Func<AutoFunctionInvocationContext, Task> next)
//    {
//        var args = string.Join(
//            ", ",
//            context.Arguments?.Select(x => $"{x.Key}: {x.Value}") ?? []);
//        Console.WriteLine($"{context.Function.Name}({args}) を呼んでもいいですか？(y/n)");
//        var answer = Console.ReadLine() ?? "n";
//        if (answer.ToLower(CultureInfo.InvariantCulture) == "y")
//        {
//            await next(context);
//        }
//        else
//        {
//            context.Result = new(context.Result, "ユーザーがキャンセルしました。");
//            context.Terminate = true;
//        }
//    }
//}
