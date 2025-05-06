using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Agents;
public record AgentRequest(ICollection<ChatMessageContent> Messages);
