
using Azure.Identity;
using Microsoft.Extensions.Configuration;
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
// Chat Completions API 用のサービスを追加
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

// Semantic Kernel のコアクラスの Kernel クラスを作成
var kernel = builder.Build();

// Chat Completions API を呼び出すサービスを取得
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Chat Completions API に渡すメッセージを作成
ChatHistory chatHistory = [
    // system プロンプト
    new ChatMessageContent(AuthorRole.System,
        $"""
        あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
        ユーザーからの質問には以下のコンテキストに書いてある内容から回答をしてください。
        コンテキストに書いていないことに関しては「チュールが美味しい」という話題に誘導するような雑談をしてください。
                
        ### コンテキスト
        - 東京の天気は晴れです。
        """),
    // user プロンプト
    new ChatMessageContent(AuthorRole.User,
        """
        こんにちは、私の名前はかずきです。
        東京の天気を教えてください。
        """),
];

// チャットのメッセージを渡して結果を受け取る
var result = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory, 
    kernel: kernel);

// 結果を表示
Console.WriteLine(result.Content);
