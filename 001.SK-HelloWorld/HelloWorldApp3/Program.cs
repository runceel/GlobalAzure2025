
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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

// デフォルトのプロンプトのテンプレートエンジンのファクトリーを登録
builder.Services.AddSingleton<IPromptTemplateFactory, KernelPromptTemplateFactory>();
// WeatherPlugin クラスをプラグインとして登録
builder.Plugins.AddFromType<WeatherPlugin>();

var kernel = builder.Build();

// テンプレートエンジンのファクトリーを取得
var f = kernel.GetRequiredService<IPromptTemplateFactory>();
// Chat Completions API を呼び出すサービスを取得
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
// パラメーターを作成
var arguments = new KernelArguments
{
    ["name"] = "セマンティックカーネル",
    ["location"] = "東京",
};

// テンプレートエンジンの API を直で使ってプロンプトをレンダリング
// Chat Completions API に渡すメッセージの
ChatHistory chatHistory = [
    new ChatMessageContent(AuthorRole.System,
        await f.Create(new("""
            あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
            ユーザーからの質問には以下のコンテキストに書いてある内容から回答をしてください。
            コンテキストに書いていないことに関しては「チュールが美味しい」という話題に誘導するような雑談をしてください。
            
            ### コンテキスト
            - {{WeatherPlugin.GetWeatherForecast $location}}
            """)).RenderAsync(kernel, arguments)),
    new ChatMessageContent(AuthorRole.User,
        await f.Create(new("""
            こんにちは、私の名前は {{$name}} です。
            {{$location}} の天気を教えてください。
            """)).RenderAsync(kernel, arguments)),
];

// チャットのメッセージを渡して結果を受け取る
var result = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory, 
    kernel: kernel);

// 結果を表示
Console.WriteLine(result.Content);

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
