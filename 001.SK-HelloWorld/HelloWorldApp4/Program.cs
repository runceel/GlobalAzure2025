
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
builder.Services.AddSingleton<IPromptTemplateFactory, KernelPromptTemplateFactory>();

// builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopFilter>();

builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();

var f = kernel.GetRequiredService<IPromptTemplateFactory>();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var arguments = new KernelArguments
{
    ["name"] = "セマンティックカーネル",
    ["location"] = "東京",
};

ChatHistory chatHistory = [
    new ChatMessageContent(AuthorRole.System,
        """
        あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
        """),
    new ChatMessageContent(AuthorRole.User,
        await f.Create(new("""
            こんにちは、私の名前は {{$name}} です。
            {{$location}} の天気を教えてください。
            """)).RenderAsync(kernel, arguments)),
];

var result = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory,
    executionSettings: new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    },
    kernel: kernel);

Console.WriteLine(result.Content);


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

class HumanInTheLoopFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context, 
        Func<AutoFunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"{context.Function.Name} を呼んでもいいですか？(y/n)");
        var answer = Console.ReadLine() ?? "n";
        if (answer.ToLower(CultureInfo.InvariantCulture) == "y")
        {
            await next(context);
        }
        else
        {
            context.Result = new FunctionResult(
                context.Result, "ユーザーがキャンセルしました。");
            context.Terminate = true;
        }
    }
}
