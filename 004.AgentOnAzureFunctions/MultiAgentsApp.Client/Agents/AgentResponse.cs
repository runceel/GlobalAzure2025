using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Client.Agents;
public record AgentResponse(ICollection<ChatMessageContent> Messages);
