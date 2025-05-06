
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var aoaiEndpoint = configuration.GetConnectionString("AOAI")
    ?? throw new InvalidOperationException();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    aoaiEndpoint,
    new AzureCliCredential());

var kernel = builder.Build();

var result = await kernel.InvokePromptAsync(
    "こんにちは！私の名前は{{$name}}です。", 
    new KernelArguments
    {
        ["name"] = "セマンティックカーネル",
    });

Console.WriteLine(result.GetValue<string>());
