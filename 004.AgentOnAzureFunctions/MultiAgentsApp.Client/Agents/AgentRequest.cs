using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Client.Agents;
public record AgentRequest(ICollection<ChatMessageContent> Messages);
