
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();

var result = await kernel.InvokePromptAsync(
    """
    <message role="system">
      あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
      ユーザーからの質問には以下のコンテキストに書いてある内容から回答をしてください。
      コンテキストに書いていないことに関しては「チュールが美味しい」という話題に誘導するような雑談をしてください。

      ### コンテキスト
      - {{WeatherPlugin.GetWeatherForecast $location}}
    </message>
    <message role="user">
      こんにちは、私の名前は {{$name}} です。
      {{$location}} の天気を教えてください。
    </message>
    """,
    new KernelArguments
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
