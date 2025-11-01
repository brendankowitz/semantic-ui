# SemanticUI - Project Summary

## What is SemanticUI?

SemanticUI is a groundbreaking framework that transforms how users interact with healthcare data. Instead of pre-built, static interfaces, SemanticUI uses AI to **dynamically generate custom React components** based on conversational context.

**Example:**
```
User: "I need to edit patient John Doe's contact information"

Traditional App: Navigate through menus → Find patient → Click edit → Use generic form

SemanticUI: AI generates a custom form specifically for John Doe's contact info → Renders securely → Ready to use
```

## Core Innovation

**The LLM acts as a real-time application generator**, creating purpose-built UIs on-demand:

- Need a patient form? → **Generated instantly**
- Want a population chart? → **Created on the fly**
- Require a custom dashboard? → **Built for your exact needs**

## Key Features

### 1. Dynamic UI Generation
- LLM generates React components in real-time
- FHIR-compliant healthcare interfaces
- Tailwind CSS styling
- Full interactivity

### 2. Multi-Layer Security
- ✅ AST validation (client + server)
- ✅ DOMPurify sanitization
- ✅ Sandpack iframe isolation
- ✅ CSP headers
- ✅ Rate limiting

### 3. Real-Time Streaming
- SignalR WebSocket communication
- Streaming chat responses
- Live UI component delivery
- Interactive feedback loop

### 4. FHIR Integration
- Patient management
- Observation entry
- Population analytics
- Clinical timelines
- Resource operations

## Technology Stack

### Backend
```
.NET 8 Web API
├─ SignalR (real-time)
├─ Semantic Kernel (LLM orchestration)
├─ Entity Framework Core (ORM)
├─ PostgreSQL (database)
└─ Redis (caching/backplane)
```

### Frontend
```
React 18 + TypeScript
├─ Sandpack (secure rendering)
├─ SignalR Client (WebSocket)
├─ Acorn (AST validation)
├─ DOMPurify (sanitization)
└─ Tailwind CSS (styling)
```

### AI
```
OpenAI GPT-4
├─ Semantic Kernel plugins
├─ FHIR UI generation
├─ Data operations
└─ Context management
```

## Project Structure

```
semantic-ui/
├── src/
│   ├── SemanticUI.Api/              # .NET Backend
│   │   ├── Controllers/             # REST endpoints
│   │   ├── Hubs/                    # SignalR ChatHub
│   │   ├── Services/                # Business logic
│   │   │   ├── ChatService.cs       # Main orchestration
│   │   │   ├── FhirUIGenerationPlugin.cs
│   │   │   └── FhirDataPlugin.cs
│   │   ├── Security/                # Code validation
│   │   ├── Models/                  # Database entities
│   │   ├── Data/                    # DbContext
│   │   └── Program.cs               # App configuration
│   │
│   ├── SemanticUI.Core/             # Shared library
│   │   ├── Models/                  # DTOs
│   │   ├── Interfaces/              # Contracts
│   │   └── DTOs/                    # API responses
│   │
│   └── SemanticUI.Web/              # React Frontend
│       ├── src/
│       │   ├── components/
│       │   │   ├── ChatInterface.tsx
│       │   │   └── DynamicUIRenderer.tsx
│       │   ├── services/
│       │   │   ├── signalRService.ts
│       │   │   ├── apiService.ts
│       │   │   └── codeValidation.ts
│       │   ├── types/               # TypeScript types
│       │   └── App.tsx
│       └── package.json
│
├── examples/                        # Sample components
│   ├── PatientFormExample.jsx
│   └── PopulationChartExample.jsx
│
├── docs/
│   ├── README.md                    # Overview
│   ├── GETTING_STARTED.md           # Setup guide
│   ├── ARCHITECTURE.md              # Technical details
│   ├── EXAMPLES.md                  # Use cases
│   └── PROJECT_SUMMARY.md           # This file
│
├── docker-compose.yml               # Local dev environment
└── SemanticUI.sln                   # Visual Studio solution
```

## How It Works

### 1. User Sends Message
```typescript
// Frontend
const handleSend = async () => {
  await signalRService.streamMessage(chatId, input);
};
```

### 2. Backend Processes with Semantic Kernel
```csharp
// Backend
await foreach (var update in _chatCompletion.GetStreamingChatMessageContentsAsync(
    history, settings, _kernel))
{
    // LLM can call FHIR plugins automatically
    // Example: GetPatient, GeneratePatientForm, etc.
}
```

### 3. LLM Generates UI Component
```xml
<uiComponent id="patient-form-001" type="form">
```jsx
import React, { useState } from 'react';
import { submitToChat } from './utils';

export default function PatientForm() {
  // FHIR-compliant patient form
  // Tailwind CSS styling
  // Validation logic
  // submitToChat on submit
}
```
</uiComponent>
```

### 4. Backend Validates & Streams
```csharp
// Extract UI definition
var (text, uiDef) = ExtractUIDefinition(content);

// Validate code
var validation = _codeValidation.ValidateReactCode(uiDef.Code);

// Stream to client
yield return new StreamChunk {
    Type = ChunkType.UIComponent,
    UIDefinition = uiDef
};
```

### 5. Frontend Validates & Renders
```typescript
// AST validation
const astResult = validateCode(uiDefinition.code);

// DOMPurify sanitization
const sanitized = DOMPurify.sanitize(uiDefinition.code);

// Render in Sandpack (isolated iframe)
<Sandpack files={{ '/App.js': validatedCode }} />
```

### 6. User Interacts
```jsx
// Inside generated component
const handleSubmit = (data) => {
  submitToChat({ action: 'savePatient', patient: data });
};
```

### 7. Backend Processes Interaction
```csharp
// ChatHub receives interaction
public async Task SubmitUIPayload(string componentId, JsonElement payload)
{
    // Process with LLM
    var response = await _chatService.ProcessUIInteractionAsync(...);

    // Continue conversation
}
```

## Security Model

```
┌─────────────────────────────────────┐
│  Generated Code                     │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Layer 1: Server AST (Esprima)      │
│  - Blocks: eval, Function, etc.     │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Layer 2: Client AST (Acorn)        │
│  - Validates syntax & patterns      │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Layer 3: DOMPurify                 │
│  - Sanitizes HTML content           │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Layer 4: Sandpack Iframe           │
│  - Process isolation                │
│  - No parent DOM access             │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Layer 5: CSP Headers               │
│  - Content Security Policy          │
└──────────────┬──────────────────────┘
               │
               ▼
         Safe Execution ✅
```

## FHIR Capabilities

### Available Plugins

**UI Generation:**
- `GeneratePatientForm` - Patient data entry/edit
- `GeneratePatientTable` - Patient lists
- `GenerateObservationForm` - Vital signs, labs
- `GeneratePatientTimeline` - Clinical history
- `GeneratePopulationChart` - Analytics visualizations
- `GenerateDashboard` - Metrics overview

**Data Operations:**
- `SearchPatients` - Find patients by criteria
- `GetPatient` - Retrieve patient details
- `GetPatientObservations` - Get vitals, labs
- `GetPopulationStats` - Aggregated statistics
- `SavePatient` - Create/update patients
- `SaveObservation` - Record observations

### Example Workflow

```
User: "Show me diabetes patients over 50, then create a chart"

1. LLM calls SearchPatients("condition:diabetes,age:>50")
2. Receives patient list
3. LLM calls GetPopulationStats for age distribution
4. LLM calls GeneratePopulationChart
5. Generates Recharts component with data
6. User sees interactive chart
7. User clicks a bar
8. LLM generates patient table for that segment
```

## Quick Start

### 1. Prerequisites
```bash
.NET 8 SDK
Node.js 18+
PostgreSQL (optional)
Redis (optional)
OpenAI API key
```

### 2. Start Backend
```bash
cd src/SemanticUI.Api
# Edit appsettings.json with your OpenAI key
dotnet run
# API runs on https://localhost:5001
```

### 3. Start Frontend
```bash
cd src/SemanticUI.Web
npm install
npm run dev
# App runs on http://localhost:5173
```

### 4. Optional: Start Dependencies
```bash
docker-compose up -d
# Starts PostgreSQL and Redis
```

## Use Cases

### Healthcare Provider Portal
- Patient lookup and editing
- Vital signs entry
- Lab results visualization
- Population health dashboards

### Clinical Research
- Cohort identification
- Data visualization
- Export capabilities
- Timeline analysis

### EHR Integration
- FHIR-compliant operations
- Real-time data entry
- Custom forms for workflows
- Audit trails

### Population Health Management
- Analytics dashboards
- Trend visualization
- Risk stratification
- Quality metrics

## Extension Points

### 1. Add Custom Plugins
```csharp
public class MyCustomPlugin
{
    [KernelFunction, Description("My custom function")]
    public string DoSomething(string input)
    {
        // Your logic
        return result;
    }
}

// Register
kernel.Plugins.AddFromType<MyCustomPlugin>();
```

### 2. Connect Real FHIR Server
```csharp
// Replace mock implementation
var client = new FhirClient("https://your-server/");
var patient = await client.ReadAsync<Patient>($"Patient/{id}");
```

### 3. Custom UI Templates
- Add to `FhirUIGenerationPlugin`
- Define generation prompt
- Document in system prompt

## Performance Considerations

### Token Usage
- Average UI generation: ~1,500 tokens
- Use GPT-3.5 for simple queries
- Cache common components
- Set max token limits

### Scalability
- Redis backplane for SignalR
- Horizontal scaling supported
- Database connection pooling
- Rate limiting per user

### Caching Strategy
```csharp
// Cache generated components
await _cache.SetStringAsync(
    $"component-{type}-{id}",
    JsonSerializer.Serialize(component),
    new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }
);
```

## Deployment

### Recommended Stack
```
Azure Front Door (React SPA)
    ↓
Azure App Service (API)
    ↓
Azure Redis Cache (SignalR)
    ↓
Azure Database for PostgreSQL
```

### Environment Variables
```bash
# Backend
OpenAI__ApiKey=sk-...
ConnectionStrings__DefaultConnection=...
ConnectionStrings__Redis=...
Jwt__Key=...

# Frontend
VITE_API_URL=https://api.example.com
VITE_SIGNALR_HUB=/chatHub
```

## Future Enhancements

1. **Component Library** - Reuse successful generations
2. **Multi-Modal** - Add image/diagram generation
3. **Collaboration** - Multi-user chat sessions
4. **Version Control** - Track component evolution
5. **A/B Testing** - Compare UI variations
6. **Offline Mode** - Cached components
7. **Mobile App** - React Native version

## Key Benefits

### For Users
- ✅ No navigation through complex menus
- ✅ Context-aware interfaces
- ✅ Natural language interaction
- ✅ Instant customization

### For Developers
- ✅ Reduced UI development time
- ✅ Extensible plugin architecture
- ✅ Production-ready security
- ✅ Scalable infrastructure

### For Organizations
- ✅ Faster time to market
- ✅ Adaptable to changing needs
- ✅ FHIR compliance
- ✅ Audit trails

## Conclusion

SemanticUI represents a paradigm shift in application development:

**Old Way:**
```
Requirements → Design → Code → Test → Deploy → User
(Weeks to months)
```

**SemanticUI Way:**
```
User Request → AI Generation → Validation → Rendering
(Seconds)
```

By treating the LLM as a **dynamic application generator** rather than just a chatbot, SemanticUI enables unprecedented flexibility and speed in creating healthcare applications.

## Resources

- **Getting Started:** See `GETTING_STARTED.md`
- **Architecture:** See `ARCHITECTURE.md`
- **Examples:** See `EXAMPLES.md`
- **Code Examples:** See `/examples` directory

## License

MIT License - See LICENSE file

## Support

For questions and issues, please use the GitHub issue tracker.

---

**Built with ❤️ for healthcare innovation**
