
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;

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

// WeatherPlugin クラスをプラグインとして登録
builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();

// プロンプトを実行して結果を取得
var result = await kernel.InvokePromptAsync(
    // プロンプトテンプレートの定義
    // message タグでチャット形式のメッセージを定義
    // プラグインの関数を呼び出して System プロンプトに埋め込む (RAG)
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
    // プロンプトの引数を指定
    new KernelArguments
    {
        ["name"] = "セマンティックカーネル",
        ["location"] = "東京",
    });

// 結果を表示
Console.WriteLine(result.GetValue<string>());


// WeatherPlugin クラスを定義
class WeatherPlugin
{
    // 指定した場所の天気予報を取得する関数を定義
    [KernelFunction]
    [Description("天気予報を取得します。")]
    public string GetWeatherForecast(
        [Description("場所")]
        string location)
    {
        return $"{location} の天気は「晴れ」です。";
    }
}
