using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace MultiAgentsApp.Agents;
public record AgentResponse(ICollection<ChatMessageContent> Messages);
