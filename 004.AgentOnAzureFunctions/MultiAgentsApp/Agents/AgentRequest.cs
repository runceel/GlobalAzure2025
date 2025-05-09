using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Agents;

// Agent のリクエストを表すクラス
public record AgentRequest(ICollection<ChatMessageContent> Messages);
