using System.Text.Json;
using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Client.Agents;

public class WriterAgentClient(HttpClient httpClient, ILogger<WriterAgentClient> logger)
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AgentResponse> CallAsync(AgentRequest request,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/AgentFunction",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadFromJsonAsync<HttpManagementPayload>(
            s_jsonSerializerOptions,
            cancellationToken: cancellationToken);
        if (responseBody is null)
        {
            throw new InvalidOperationException("Response body is null");
        }

        string latestCustomStatus = "";
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(500);

            var orchestrationMetadata = await httpClient.GetFromJsonAsync<OrchestrationMetadata>(
                responseBody.StatusQueryGetUri,
                s_jsonSerializerOptions,
                cancellationToken);
            if (orchestrationMetadata is null)
            {
                throw new InvalidOperationException("Orchestration metadata is null");
            }

            if (orchestrationMetadata.RuntimeStatus == "Completed")
            {
                if (orchestrationMetadata.Output == null)
                {
                    throw new InvalidOperationException("Output is null");
                }

                return orchestrationMetadata.Output;
            }
            else if (!string.IsNullOrEmpty(orchestrationMetadata.CustomStatus))
            {
                var currentCustomStatus = orchestrationMetadata.CustomStatus;
                if (currentCustomStatus != null && latestCustomStatus != currentCustomStatus)
                {
                    progress.Report(latestCustomStatus = currentCustomStatus);
                }
            }
        }

        throw new OperationCanceledException("Operation was canceled", cancellationToken);
    }

    record HttpManagementPayload(
        string StatusQueryGetUri);
    record OrchestrationMetadata(
        string RuntimeStatus,
        AgentResponse? Output,
        string? CustomStatus);
}
