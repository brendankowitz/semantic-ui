using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticUI.Api.Data;
using SemanticUI.Core.Interfaces;
using SemanticUI.Core.Models;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace SemanticUI.Api.Services;

public class ChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly AppDbContext _dbContext;
    private readonly ICodeValidationService _codeValidation;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        Kernel kernel,
        IChatCompletionService chatCompletion,
        AppDbContext dbContext,
        ICodeValidationService codeValidation,
        ILogger<ChatService> logger)
    {
        _kernel = kernel;
        _chatCompletion = chatCompletion;
        _dbContext = dbContext;
        _codeValidation = codeValidation;
        _logger = logger;
    }

    public async IAsyncEnumerable<StreamChunk> StreamResponseAsync(
        string chatId,
        string userId,
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatGuid = Guid.Parse(chatId);

        // Get or create chat history
        var history = await GetOrCreateChatHistoryAsync(chatGuid, userId);

        // Add user message to history
        history.AddUserMessage(prompt);

        // Save user message to database
        var userMessage = new Models.ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatGuid,
            Role = "user",
            Content = prompt,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Messages.Add(userMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Configure execution settings with FHIR context
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7,
            MaxTokens = 2000
        };

        var textContent = new StringBuilder();
        UIDefinition? currentUIComponent = null;
        var hasYieldedContent = false;

        // Stream the response
        await foreach (var update in _chatCompletion.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings,
            _kernel,
            cancellationToken))
        {
            if (update.Content != null)
            {
                textContent.Append(update.Content);

                // Check for UI component markers
                var (remainingText, uiDef) = ExtractUIDefinition(textContent.ToString());

                if (uiDef != null && currentUIComponent == null)
                {
                    currentUIComponent = uiDef;

                    // Validate the UI component code
                    var validationResult = _codeValidation.ValidateReactCode(uiDef.Code);

                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Generated UI component failed validation: {Violations}",
                            string.Join(", ", validationResult.Violations.Select(v => v.Message)));

                        yield return new StreamChunk
                        {
                            Type = ChunkType.Error,
                            Content = "The generated UI component contains security violations and cannot be rendered."
                        };

                        continue;
                    }

                    // Save UI component to database
                    var uiComponent = new Models.UIComponent
                    {
                        Id = Guid.Parse(uiDef.ComponentId),
                        ChatId = chatGuid,
                        ComponentType = uiDef.Type,
                        Code = uiDef.Code,
                        PropsJson = JsonSerializer.Serialize(uiDef.Props),
                        DependenciesJson = JsonSerializer.Serialize(uiDef.Dependencies),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.UIComponents.Add(uiComponent);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Yield UI component chunk
                    yield return new StreamChunk
                    {
                        Type = ChunkType.UIComponent,
                        UIDefinition = uiDef
                    };

                    hasYieldedContent = true;
                }
                else if (!string.IsNullOrEmpty(update.Content) && currentUIComponent == null)
                {
                    // Yield text chunk
                    yield return new StreamChunk
                    {
                        Type = ChunkType.Text,
                        Content = update.Content
                    };

                    hasYieldedContent = true;
                }
            }
        }

        // Save final assistant message
        var finalContent = textContent.ToString();
        history.AddAssistantMessage(finalContent);

        var assistantMessage = new Models.ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatGuid,
            Role = "assistant",
            Content = finalContent,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Messages.Add(assistantMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update chat timestamp
        var chat = await _dbContext.Chats.FindAsync(chatGuid);
        if (chat != null)
        {
            chat.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Microsoft.SemanticKernel.ChatCompletion.ChatHistory> GetOrCreateChatHistoryAsync(
        Guid chatId,
        string userId)
    {
        var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

        // Add system prompt for FHIR UI generation
        history.AddSystemMessage(GetSystemPrompt());

        // Load existing messages from database
        var messages = await _dbContext.Messages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .Take(50) // Limit to last 50 messages to manage context
            .ToListAsync();

        foreach (var msg in messages)
        {
            if (msg.Role == "user")
                history.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant")
                history.AddAssistantMessage(msg.Content);
            else if (msg.Role == "system")
                history.AddSystemMessage(msg.Content);
        }

        // Create chat if it doesn't exist
        var chat = await _dbContext.Chats.FindAsync(chatId);
        if (chat == null)
        {
            chat = new Models.Chat
            {
                Id = chatId,
                UserId = userId,
                Title = "New Chat",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Chats.Add(chat);
            await _dbContext.SaveChangesAsync();
        }

        return history;
    }

    private (string text, UIDefinition? uiDef) ExtractUIDefinition(string content)
    {
        // Pattern: <uiComponent id="..." type="...">```jsx ... ```</uiComponent>
        var pattern = @"<uiComponent\s+id=""(?<id>[^""]+)""\s+type=""(?<type>[^""]+)""(?:\s+deps=""(?<deps>[^""]*)"")?\s*>\s*```(?:jsx|javascript|tsx?)\s*(?<code>.*?)```\s*</uiComponent>";
        var match = Regex.Match(content, pattern, RegexOptions.Singleline);

        if (!match.Success)
            return (content, null);

        var componentId = match.Groups["id"].Value;
        var type = match.Groups["type"].Value;
        var code = match.Groups["code"].Value.Trim();
        var depsStr = match.Groups["deps"].Value;

        var dependencies = new Dictionary<string, string>
        {
            { "react", "^18.2.0" },
            { "react-dom", "^18.2.0" }
        };

        // Parse additional dependencies if provided
        if (!string.IsNullOrEmpty(depsStr))
        {
            foreach (var dep in depsStr.Split(','))
            {
                var parts = dep.Split(':');
                if (parts.Length == 2)
                {
                    dependencies[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        var uiDef = new UIDefinition
        {
            ComponentId = componentId,
            Type = type,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            Dependencies = dependencies
        };

        var textWithoutUI = content.Replace(match.Value, "").Trim();
        return (textWithoutUI, uiDef);
    }

    public async Task<bool> UserHasAccessAsync(string chatId, string userId)
    {
        var chatGuid = Guid.Parse(chatId);
        var chat = await _dbContext.Chats.FindAsync(chatGuid);
        return chat != null && chat.UserId == userId;
    }

    public async Task<string?> GetChatIdForComponentAsync(string componentId)
    {
        var componentGuid = Guid.Parse(componentId);
        var component = await _dbContext.UIComponents.FindAsync(componentGuid);
        return component?.ChatId.ToString();
    }

    public async Task<ProcessedResponse> ProcessUIInteractionAsync(
        string chatId,
        string componentId,
        JsonElement payload,
        string userId)
    {
        var chatGuid = Guid.Parse(chatId);
        var componentGuid = Guid.Parse(componentId);

        // Save interaction to database
        var interaction = new Models.UIInteraction
        {
            Id = Guid.NewGuid(),
            ComponentId = componentGuid,
            UserId = userId,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.UIInteractions.Add(interaction);
        await _dbContext.SaveChangesAsync();

        // Get chat history
        var history = await GetOrCreateChatHistoryAsync(chatGuid, userId);

        var interactionMessage = $@"
The user interacted with UI component '{componentId}' and submitted the following data:

{JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })}

Process this interaction, perform any necessary FHIR operations, and respond appropriately.
";

        history.AddUserMessage(interactionMessage);

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7
        };

        var response = await _chatCompletion.GetChatMessageContentAsync(
            history,
            settings,
            _kernel);

        // Save messages
        var userMsg = new Models.ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatGuid,
            Role = "user",
            Content = interactionMessage,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Messages.Add(userMsg);

        var assistantMsg = new Models.ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatGuid,
            Role = "assistant",
            Content = response.Content ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Messages.Add(assistantMsg);

        await _dbContext.SaveChangesAsync();

        return new ProcessedResponse
        {
            Content = response.Content ?? string.Empty,
            Metadata = response.Metadata
        };
    }

    private string GetSystemPrompt()
    {
        return @"You are a helpful AI assistant specialized in FHIR (Fast Healthcare Interoperability Resources) data management. You help users work with healthcare data by generating custom, interactive React interfaces when beneficial.

## When to Generate UI Components:

Generate UI components when the user needs to:
- View or edit FHIR Patient resources
- Enter clinical observations or other FHIR resources
- View tabular health data
- Visualize population health statistics with charts
- Explore patient clinical timelines
- View healthcare dashboards and metrics

## UI Component Format:

When generating a UI component, use this exact format:

<uiComponent id=""unique-component-id"" type=""form|table|chart|timeline|dashboard"" deps=""package:version,package:version"">
```jsx
import React, { useState } from 'react';
import { submitToChat } from './utils';

export default function ComponentName() {
  // Your React component code here
  // Use submitToChat(data) to send results back to the chat
  return (
    <div className=""p-4"">
      {/* Component UI */}
    </div>
  );
}
```
</uiComponent>

## Requirements:

1. **React 18** with hooks only (no class components)
2. **Tailwind CSS** for all styling (utility classes only, no custom CSS)
3. **FHIR R4** specification compliance for healthcare data
4. Call **submitToChat(payload)** to send data back
5. Include proper error handling and loading states
6. Make components responsive and accessible
7. Use semantic HTML and ARIA attributes
8. Include form validation with inline error messages

## Available Plugins:

You have access to these FHIR plugins that you should use:
- GeneratePatientForm - Creates forms for editing FHIR Patient resources
- GeneratePatientTable - Creates tables for displaying patient lists
- GenerateObservationForm - Creates forms for clinical observations
- GeneratePatientTimeline - Creates timeline views of patient history
- GeneratePopulationChart - Creates charts for population health analytics
- GenerateDashboard - Creates healthcare metric dashboards
- SearchPatients - Searches for patients
- GetPatient - Gets detailed patient information
- GetPatientObservations - Gets patient clinical observations
- GetPopulationStats - Gets population health statistics
- SavePatient - Saves patient data to FHIR server
- SaveObservation - Saves observation data

## Example - Patient Edit Form:

User: ""I need to edit patient John Doe's information""

You should:
1. Use GetPatient to fetch current patient data
2. Use GeneratePatientForm to create an edit form pre-populated with data
3. When user submits, use SavePatient to save changes

Generate clean, production-ready, secure code that follows FHIR and React best practices.";
    }
}
