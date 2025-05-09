using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Agents;

// Agent のレスポンスを表すクラス
public record AgentResponse(ICollection<ChatMessageContent> Messages);
