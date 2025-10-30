# Getting Started with SemanticUI

This guide will help you get SemanticUI up and running on your local machine.

## Prerequisites

- .NET 8 SDK or later
- Node.js 18+ and npm
- PostgreSQL 15+ (optional - will use in-memory DB if not configured)
- Redis 7+ (optional - for production SignalR scaling)
- OpenAI API key

## Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd semantic-ui
```

### 2. Configure the Backend

Navigate to the API project and update the configuration:

```bash
cd src/SemanticUI.Api
```

Edit `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-actual-openai-api-key",
    "Model": "gpt-4"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=semanticui;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379"
  }
}
```

**Note**: If you don't configure PostgreSQL or Redis, the application will use in-memory alternatives for development.

### 3. Start the Backend

```bash
dotnet restore
dotnet run
```

The API will start on `https://localhost:5001` (or `http://localhost:5000`)

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### 4. Start the Frontend

Open a new terminal and navigate to the Web project:

```bash
cd src/SemanticUI.Web
```

Install dependencies:

```bash
npm install
```

Start the development server:

```bash
npm run dev
```

The frontend will start on `http://localhost:5173`

### 5. Open the Application

Open your browser and navigate to:
```
http://localhost:5173
```

## First Steps

### Example 1: View Patient Information

In the chat interface, try:
```
Show me patient John Doe's information
```

The LLM will:
1. Use the `GetPatient` function to fetch patient data
2. Generate a custom form pre-populated with the patient's data
3. Render it securely in the browser

### Example 2: Population Analytics

Try asking:
```
Create a chart showing patient age distribution by gender
```

The LLM will:
1. Use the `GetPopulationStats` function
2. Generate a custom Recharts component
3. Display an interactive bar chart

### Example 3: Edit Patient

Try:
```
I need to update patient John Doe's contact information
```

The LLM will create an editable form that you can fill out and submit.

## Understanding the Flow

1. **User sends message** → Chat interface
2. **Message sent via SignalR** → Backend ChatHub
3. **ChatService processes with Semantic Kernel** → Calls FHIR plugins as needed
4. **LLM generates response** → May include UI component definition
5. **Response streams back** → Via SignalR to frontend
6. **UI component validated and rendered** → Sandpack displays it securely
7. **User interacts with component** → Submits data back via SignalR
8. **Cycle repeats** → LLM processes the interaction

## Architecture Overview

```
┌─────────────────────────────────────┐
│   Browser (React + SignalR)         │
│   - Chat Interface                  │
│   - Dynamic UI Renderer (Sandpack)  │
│   - AST Validation                  │
└──────────────┬──────────────────────┘
               │ WebSocket
┌──────────────▼──────────────────────┐
│   .NET Web API                      │
│   - ChatHub (SignalR)               │
│   - ChatService                     │
│   - Semantic Kernel                 │
│   - FHIR Plugins                    │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   OpenAI API (GPT-4)                │
│   - Dynamic UI Generation           │
│   - FHIR Data Processing            │
└─────────────────────────────────────┘
```

## FHIR Plugins

SemanticUI includes these FHIR-specific plugins:

**UI Generation:**
- `GeneratePatientForm` - Creates patient edit forms
- `GeneratePatientTable` - Creates patient list tables
- `GenerateObservationForm` - Creates observation entry forms
- `GeneratePatientTimeline` - Creates clinical timelines
- `GeneratePopulationChart` - Creates analytics charts
- `GenerateDashboard` - Creates metric dashboards

**Data Operations:**
- `SearchPatients` - Searches for patients
- `GetPatient` - Gets patient details
- `GetPatientObservations` - Gets clinical observations
- `GetPopulationStats` - Gets population statistics
- `SavePatient` - Saves patient data
- `SaveObservation` - Saves observations

## Security Features

SemanticUI implements multiple security layers:

1. **AST Validation** - Both client and server validate generated code
2. **Sandpack Isolation** - UI components run in isolated iframes
3. **DOMPurify Sanitization** - HTML is sanitized before rendering
4. **CSP Headers** - Content Security Policy prevents XSS
5. **Rate Limiting** - Prevents abuse
6. **JWT Authentication** - Secure API access (when configured)

## Troubleshooting

### Backend won't start
- Ensure .NET 8 SDK is installed: `dotnet --version`
- Check that port 5000/5001 is not in use
- Verify OpenAI API key is set in appsettings.json

### Frontend won't start
- Ensure Node.js 18+ is installed: `node --version`
- Delete `node_modules` and run `npm install` again
- Check that port 5173 is not in use

### SignalR connection fails
- Verify the backend is running
- Check browser console for errors
- Ensure CORS is properly configured

### UI components won't render
- Check browser console for security violations
- Verify the generated code passes AST validation
- Look for errors in the Sandpack iframe

## Next Steps

- Explore the example components in `/examples`
- Read the full specification document
- Customize the system prompt in `ChatService.cs`
- Add your own FHIR plugins
- Connect to a real FHIR server (currently uses mock data)

## Support

For issues and questions, please use the GitHub issue tracker.
