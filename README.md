# SemanticUI - Dynamic LLM-Driven FHIR Application Framework

SemanticUI is a comprehensive framework that combines .NET, Semantic Kernel, and React to create dynamic, LLM-generated user interfaces for FHIR (Fast Healthcare Interoperability Resources) server operations.

## Overview

Based on user conversations, the LLM dynamically assembles custom React components to assist with various tasks:
- **Patient Management**: Edit patient records with auto-generated forms
- **Population Queries**: Explore health data with dynamically created charts and visualizations
- **Data Entry**: Create context-aware forms based on FHIR resource types
- **Analytics**: Generate interactive dashboards for health data analysis

## Architecture

```
┌─────────────────────────────────────┐
│     React 18 SPA (Browser)          │
│  - Chat Interface                   │
│  - Dynamic UI Renderer (Sandpack)   │
│  - SignalR Client                   │
└──────────────┬──────────────────────┘
               │ HTTPS/WebSocket
┌──────────────▼──────────────────────┐
│  .NET 8 Web API + SignalR           │
│  - ChatHub (Streaming)              │
│  - Semantic Kernel                  │
│  - FHIR Plugins                     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  PostgreSQL + Redis                 │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  OpenAI API (GPT-4)                 │
└─────────────────────────────────────┘
```

## Project Structure

```
semantic-ui/
├── src/
│   ├── SemanticUI.Api/              # .NET Web API + SignalR
│   │   ├── Controllers/
│   │   ├── Hubs/
│   │   ├── Services/
│   │   ├── Models/
│   │   └── Program.cs
│   ├── SemanticUI.Core/             # Shared models and interfaces
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   └── DTOs/
│   └── SemanticUI.Web/              # React SPA
│       ├── src/
│       │   ├── components/
│       │   ├── services/
│       │   ├── hooks/
│       │   └── App.tsx
│       └── package.json
├── SemanticUI.sln
└── README.md
```

## Tech Stack

### Backend
- **.NET 8** - Web API
- **SignalR** - Real-time streaming
- **Semantic Kernel** - LLM orchestration
- **Entity Framework Core** - ORM
- **PostgreSQL** - Database
- **Redis** - Caching/SignalR backplane

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **SignalR Client** - Real-time communication
- **Sandpack** - Secure code execution
- **Tailwind CSS** - Styling

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PostgreSQL 15+
- Redis 7+
- OpenAI API key

### Backend Setup

1. Navigate to the API project:
   ```bash
   cd src/SemanticUI.Api
   ```

2. Update `appsettings.json` with your configuration:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-api-key",
       "Model": "gpt-4"
     },
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=semanticui;Username=postgres;Password=password",
       "Redis": "localhost:6379"
     }
   }
   ```

3. Run database migrations:
   ```bash
   dotnet ef database update
   ```

4. Start the API:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5001`

### Frontend Setup

1. Navigate to the web project:
   ```bash
   cd src/SemanticUI.Web
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Update environment variables in `.env`:
   ```
   VITE_API_URL=https://localhost:5001
   VITE_SIGNALR_HUB=/chatHub
   ```

4. Start the development server:
   ```bash
   npm run dev
   ```

The app will be available at `http://localhost:5173`

## FHIR Examples

### Patient Editor
User: "I need to edit patient John Doe's information"

The LLM generates a custom form with FHIR Patient resource fields, pre-populated with the patient's current data.

### Population Query
User: "Show me diabetes patients by age group with a chart"

The LLM:
1. Queries the FHIR server for relevant patients
2. Processes the data
3. Generates a custom React chart component
4. Displays the interactive visualization

## Security

SemanticUI implements multiple security layers:

1. **AST Validation** - Both client and server-side code analysis
2. **DOMPurify** - HTML sanitization
3. **Sandpack Isolation** - iframe-based code execution
4. **CSP Headers** - Content Security Policy
5. **JWT Authentication** - Secure API access
6. **Rate Limiting** - Per-user request limits

## Development

### Running Tests
```bash
# Backend
cd src/SemanticUI.Api
dotnet test

# Frontend
cd src/SemanticUI.Web
npm test
```

### Building for Production
```bash
# Backend
dotnet publish -c Release

# Frontend
npm run build
```

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please read the contributing guidelines before submitting PRs.

## Support

For issues and questions, please use the GitHub issue tracker.
