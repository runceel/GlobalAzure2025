#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using WriterAndReviewerApp;


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

// Semantic Kernel のセットアップ
var kernel = builder.Build();

// Writer と Reviewer の Agent を作成
var writerAgent = await AIAgentFactory.CreateWriterAgent(configuration, kernel);
var reviewerAgent = await AIAgentFactory.CreateReviewerAgent(kernel);

// Writer と Reviewer の Agent をグループ化
AgentGroupChat groupChat = new(writerAgent, reviewerAgent)
{
    ExecutionSettings = new()
    {
        // Writer -> Reviewer の順番で実行する Strategy を設定
        SelectionStrategy = new SequentialSelectionStrategy(),
        // Reviwer の結果が Review 完了になっていたら終了する Strategy を設定
        TerminationStrategy = new ReviewProcessTerminationStrategy
        {
            Agents = [ reviewerAgent ],
        },
    },
};

// タイトルを指定して Agent に記事を書かせる
const string userInput = ".NET 9 による C# 入門「基本文法編」";
groupChat.AddChatMessage(new(AuthorRole.User, userInput));
while (!groupChat.IsComplete)
{
    await foreach (var response in groupChat.InvokeAsync())
    {
        Console.WriteLine($"{response.AuthorName}: {response.Content}");
        Console.WriteLine();
    }
}

// 結果を取得して表示
var messages = groupChat.GetChatMessagesAsync();
var answer = await messages.FirstOrDefaultAsync(x => x.AuthorName == writerAgent.Name);

if (answer == null)
{
    Console.WriteLine("No answer found.");
    return;
}

// 結果を Markdown 形式でファイルに保存して VS Code で開く
await File.WriteAllTextAsync($"output.md", $"""
    {answer.Content}
    """);
Process.Start(new ProcessStartInfo("output.md")
{
    UseShellExecute = true,
});
