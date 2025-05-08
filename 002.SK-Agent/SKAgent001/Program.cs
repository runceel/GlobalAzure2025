using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ComponentModel;
using System.Globalization;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

//builder.Plugins.AddFromType<WeatherPlugin>();
//builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopFilter>();

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

const string userInput = "こんにちは！！東京の天気を教えて！";
AgentThread? thread = null;
await foreach (var response in agent.InvokeAsync(userInput, thread))
{
    thread = response.Thread;
    Console.WriteLine(response.Message.Content);
}


//class WeatherPlugin
//{
//    private static readonly string[] WeatherConditions = { "晴れ", "曇り", "雨", "雪", "雷雨" };
//    private static readonly Random Random = Random.Shared;

//    [KernelFunction]
//    [Description("天気予報を取得します。")]
//    public string GetWeatherForecast(
//        [Description("場所")]
//       string location)
//    {
//        var randomWeather = WeatherConditions[Random.Next(WeatherConditions.Length)];
//        return $"{location} の天気は「{randomWeather}」です。";
//    }
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
