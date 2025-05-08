
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

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

var kernel = builder.Build();

// プロンプトを実行
var result = await kernel.InvokePromptAsync(
    // プロンプトテンプレートを定義
    "こんにちは！私の名前は{{$name}}です。", 
    // プロンプトの引数を指定
    new KernelArguments
    {
        ["name"] = "セマンティックカーネル",
    });

// 結果を表示
Console.WriteLine(result.GetValue<string>());
