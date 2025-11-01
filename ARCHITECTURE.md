# SemanticUI Architecture

## Overview

SemanticUI is a dynamic, LLM-driven application framework that generates custom React components on-the-fly based on conversational context. It's specifically designed for FHIR (Fast Healthcare Interoperability Resources) operations but can be adapted for other domains.

## Core Concept

**Traditional Approach:**
```
User Request → Developer Codes UI → Deploy → User Uses
```

**SemanticUI Approach:**
```
User Request → LLM Generates UI → Validate → Render → User Interacts → Loop
```

The LLM acts as a **dynamic application generator**, creating purpose-built interfaces based on user needs in real-time.

## System Components

### 1. Frontend (React + TypeScript)

**Location:** `src/SemanticUI.Web/`

#### Key Components:

**ChatInterface** (`src/components/ChatInterface.tsx`)
- Main chat UI
- Manages message history
- Coordinates SignalR communication
- Handles streaming responses

**DynamicUIRenderer** (`src/components/DynamicUIRenderer.tsx`)
- Validates generated code (AST + DOMPurify)
- Renders components in Sandpack (isolated iframe)
- Handles component interactions via postMessage
- Reports security violations

**ChatSignalRService** (`src/services/signalRService.ts`)
- Manages WebSocket connection
- Handles streaming chat responses
- Sends UI interaction payloads
- Auto-reconnection logic

#### Data Flow:

```
User Input
  ↓
ChatInterface
  ↓
SignalR Service → WebSocket → Backend
  ↓
Streaming Response
  ↓
DynamicUIRenderer (if UI component)
  ↓
Sandpack (isolated rendering)
  ↓
User Interaction → postMessage
  ↓
SignalR Service → Backend
```

### 2. Backend (.NET 8 + Semantic Kernel)

**Location:** `src/SemanticUI.Api/`

#### Key Components:

**ChatHub** (`Hubs/ChatHub.cs`)
- SignalR hub for real-time communication
- Streams chat responses
- Handles UI interaction submissions
- Manages chat room membership

**ChatService** (`Services/ChatService.cs`)
- Orchestrates LLM interactions
- Manages chat history
- Extracts UI definitions from responses
- Validates generated code server-side
- Persists messages and components

**FhirUIGenerationPlugin** (`Services/FhirUIGenerationPlugin.cs`)
- Semantic Kernel plugin
- Provides UI generation functions
- Generates prompts for specific FHIR components

**FhirDataPlugin** (`Services/FhirDataPlugin.cs`)
- Semantic Kernel plugin
- Provides FHIR data operations
- Mock implementation (replace with real FHIR server)

**CodeValidationService** (`Security/CodeValidationService.cs`)
- AST-based code validation using Esprima
- Detects dangerous patterns
- Prevents code injection

#### Data Flow:

```
SignalR Hub receives message
  ↓
ChatService.StreamResponseAsync()
  ↓
Semantic Kernel + OpenAI
  ↓
Streaming response chunks
  ↓
Extract UI definition (regex)
  ↓
Validate code (AST)
  ↓
Save to database
  ↓
Stream to client
```

### 3. Database Layer

**Location:** `src/SemanticUI.Api/Data/`, `Models/`

#### Schema:

**Chats**
- Stores chat sessions
- Links to user
- Tracks creation/update times

**Messages**
- Stores individual messages
- Role (user/assistant/system)
- Content and metadata

**UIComponents**
- Stores generated UI components
- Code, type, props, dependencies
- Links to chat and optionally to message

**UIInteractions**
- Stores user interactions with components
- Payload data
- Audit trail

### 4. AI Integration (Semantic Kernel)

**Location:** `src/SemanticUI.Api/Program.cs`, `Services/`

#### How it Works:

1. **Kernel Setup:**
   ```csharp
   var kernel = Kernel.CreateBuilder()
       .AddOpenAIChatCompletion("gpt-4", apiKey)
       .Build();
   ```

2. **Plugin Registration:**
   ```csharp
   kernel.Plugins.AddFromType<FhirUIGenerationPlugin>("FhirUI");
   kernel.Plugins.AddFromType<FhirDataPlugin>("FhirData");
   ```

3. **Auto Function Calling:**
   ```csharp
   var settings = new OpenAIPromptExecutionSettings
   {
       ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
   };
   ```

4. **Streaming:**
   ```csharp
   await foreach (var update in chatCompletion.GetStreamingChatMessageContentsAsync(
       history, settings, kernel))
   {
       // Process chunks
   }
   ```

#### System Prompt:

The system prompt (in `ChatService.GetSystemPrompt()`) instructs the LLM to:
- Identify when UI components would be helpful
- Use specific XML format for UI definitions
- Call appropriate FHIR plugins
- Generate FHIR-compliant, accessible React components

## Security Architecture

### Multi-Layer Defense

```
Generated Code
  ↓
Layer 1: Server-Side AST Validation (Esprima)
  ↓
Layer 2: Client-Side AST Validation (Acorn)
  ↓
Layer 3: DOMPurify Sanitization
  ↓
Layer 4: Sandpack Iframe Isolation
  ↓
Layer 5: CSP Headers
  ↓
Safe Rendering
```

### Validation Rules

**Blocked Patterns:**
- `eval()` calls
- `Function` constructor
- `innerHTML` / `outerHTML` assignments
- `document.write`
- Direct `fetch` / `XMLHttpRequest` (use plugins instead)
- Prototype manipulation (`__proto__`)

### Sandpack Benefits

- **Process Isolation:** Components run in separate iframe
- **No Direct DOM Access:** Cannot access parent page
- **Message-Based Communication:** Only via `postMessage`
- **Resource Sandboxing:** Limited network access

## Communication Protocol

### UI Component Format

The LLM generates components in this format:

```xml
<uiComponent id="unique-id" type="form" deps="recharts:^2.10.0">
```jsx
import React, { useState } from 'react';
import { submitToChat } from './utils';

export default function MyComponent() {
  const handleSubmit = (data) => {
    submitToChat({ action: 'save', data });
  };

  return <div>...</div>;
}
```
</uiComponent>
```

### Interaction Protocol

Components communicate via `submitToChat`:

```javascript
// In component
submitToChat({
  action: 'savePatient',
  patient: fhirPatientResource
});

// Translates to postMessage
window.parent.postMessage({
  type: 'UI_INTERACTION',
  componentId: 'component-id',
  payload: { action: 'savePatient', patient: {...} }
}, '*');

// Frontend catches and sends to backend
signalR.invoke('SubmitUIPayload', componentId, payload);

// Backend processes
ChatService.ProcessUIInteractionAsync(...);
```

## Scalability Considerations

### Horizontal Scaling

**With Redis:**
```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString);
```

This enables:
- Multiple API instances
- SignalR backplane for message distribution
- Session affinity not required

### Performance Optimization

1. **Context Management:**
   - Limit chat history to last 50 messages
   - Summarize old conversations
   - Use sliding window

2. **Caching:**
   - Cache generated components (Redis)
   - Cache FHIR data lookups
   - Rate limiting per user

3. **Database:**
   - Indexes on `ChatId + CreatedAt`
   - Indexes on `UserId + UpdatedAt`
   - Consider read replicas

## Extension Points

### Adding New Plugins

1. Create plugin class:
   ```csharp
   public class MyPlugin
   {
       [KernelFunction, Description("Does something")]
       public string MyFunction(
           [Description("Input")] string input)
       {
           // Implementation
           return result;
       }
   }
   ```

2. Register in `Program.cs`:
   ```csharp
   kernel.Plugins.AddFromType<MyPlugin>("MyPlugin");
   ```

3. Update system prompt to mention new capabilities

### Connecting Real FHIR Server

Replace mock implementations in `FhirDataPlugin.cs`:

```csharp
[KernelFunction]
public async Task<string> GetPatient(string patientId)
{
    var client = new FhirClient("https://your-fhir-server/");
    var patient = await client.ReadAsync<Patient>($"Patient/{patientId}");
    return patient.ToJson();
}
```

### Custom UI Component Types

1. Add to `FhirUIGenerationPlugin`
2. Define generation prompt
3. Document in system prompt
4. Optionally create example in `/examples`

## Monitoring & Observability

### Recommended Logging

- LLM token usage
- Component generation failures
- Security violations
- SignalR connection metrics
- API response times

### Application Insights (Example)

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Log custom events
telemetryClient.TrackEvent("UIComponentGenerated", new Dictionary<string, string>
{
    { "Type", componentType },
    { "ChatId", chatId }
});
```

## Deployment Architecture

### Recommended Setup

```
┌─────────────────────────────┐
│   Azure Front Door / CDN    │ (React SPA)
└──────────┬──────────────────┘
           │
┌──────────▼──────────────────┐
│   Azure App Service         │ (.NET API)
│   - Auto-scaling enabled    │
│   - Always On               │
└──────────┬──────────────────┘
           │
┌──────────▼──────────────────┐
│   Azure Redis Cache         │ (SignalR backplane)
└─────────────────────────────┘
           │
┌──────────▼──────────────────┐
│   Azure Database for        │ (PostgreSQL)
│   PostgreSQL                │
└─────────────────────────────┘
```

## Cost Optimization

### LLM Usage

- Use GPT-3.5-turbo for simple queries
- Reserve GPT-4 for UI generation
- Cache generated components
- Set token limits

### Example:

```csharp
var settings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.7,
    MaxTokens = 2000, // Limit response size
    TopP = 0.9
};
```

## Future Enhancements

1. **Component Library:** Store and reuse common components
2. **A/B Testing:** Test different UI generations
3. **User Feedback:** Let users rate generated UIs
4. **Multi-Modal:** Add image/diagram generation
5. **Offline Mode:** Cache components for offline use
6. **Version Control:** Track component evolution
7. **Collaboration:** Multi-user chat sessions

## Conclusion

SemanticUI represents a paradigm shift from static to dynamic UI generation. By leveraging LLMs and secure sandboxing, it creates context-aware interfaces on-demand, drastically reducing development time and improving user experience.
