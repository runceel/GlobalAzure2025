using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MultiAgentsApp;

var builder = FunctionsApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IChatCompletionService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var aoaiEndpoint = configuration.GetConnectionString("AOAI")
        ?? throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
    return new AzureOpenAIChatCompletionService("gpt-4.1",
            aoaiEndpoint,
            new AzureCliCredential());
});

builder.Services.AddKernel();

builder.Services.AddKeyedTransient("WriterAgent", (sp, _) =>
{
    return AIAgentFactory.CreateWriterAgent(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<Kernel>());
});

builder.Services.AddKeyedTransient("ReviewerAgent", (sp, _) =>
{
    return AIAgentFactory.CreateReviewerAgent(
        sp.GetRequiredService<Kernel>());
});

builder.Build().Run();
