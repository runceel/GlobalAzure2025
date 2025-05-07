using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using MultiAgentsApp.Agents;

namespace MultiAgentsApp;
public class AgentFunction
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(AgentFunction))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        CancellationToken cancellationToken)
    {
        var agentRequest = await JsonSerializer.DeserializeAsync<AgentRequest>(
            req.Body,
            s_jsonSerializerOptions,
            cancellationToken);
        if (agentRequest is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(AgentOrchestrator),
            agentRequest,
            cancellation: cancellationToken);

        return await client.CreateCheckStatusResponseAsync(req, instanceId, cancellationToken);
    }
}
