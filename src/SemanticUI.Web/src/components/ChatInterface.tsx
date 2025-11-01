import React, { useState, useEffect, useRef } from 'react';
import { ChatSignalRService } from '../services/signalRService';
import { DynamicUIRenderer } from './DynamicUIRenderer';
import { Message, UIDefinition } from '../types';

interface ChatInterfaceProps {
  chatId: string;
}

export const ChatInterface: React.FC<ChatInterfaceProps> = ({ chatId }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [currentUI, setCurrentUI] = useState<UIDefinition | null>(null);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentAssistantMessage, setCurrentAssistantMessage] = useState('');
  const [isGeneratingUI, setIsGeneratingUI] = useState(false);
  const signalRService = useRef<ChatSignalRService>();
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const currentMessageRef = useRef<string>('');

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, currentUI, currentAssistantMessage]);

  useEffect(() => {
    const service = new ChatSignalRService();
    signalRService.current = service;

    // Detect if running as Tauri desktop app
    const isDesktopApp = typeof window !== 'undefined' && (window as any).__TAURI__ !== undefined;
    const apiUrl = isDesktopApp
      ? 'http://localhost:5050'
      : (import.meta.env.VITE_API_URL || 'http://localhost:5050');
    const hubPath = import.meta.env.VITE_SIGNALR_HUB || '/chatHub';
    const hubUrl = `${apiUrl}${hubPath}`;

    service
      .initialize(hubUrl)
      .then(() => {
        service.joinChat(chatId);

        service.onStreamStart(() => {
          // Stream is starting - reset accumulator
          currentMessageRef.current = '';
          console.log('Stream started');
        });

        service.onMessage((message) => {
          // Accumulate all chunks into ref
          currentMessageRef.current += message.content;
          setCurrentAssistantMessage(currentMessageRef.current);
          console.log('Got chunk, current content:', currentMessageRef.current.substring(0, 50) + '...');
        });

        service.onStreamComplete(() => {
          // Stream ended - save the accumulated message
          console.log('onStreamComplete called, message length:', currentMessageRef.current.length);
          console.log('Current message content (first 100 chars):', currentMessageRef.current.substring(0, 100));

          const messageContent = currentMessageRef.current;
          if (messageContent && messageContent.length > 0) {
            console.log('Adding message to messages, content length:', messageContent.length);
            setMessages((prev) => {
              const newMessages = [
                ...prev,
                {
                  id: crypto.randomUUID(),
                  role: 'assistant',
                  content: messageContent,
                  timestamp: new Date()
                }
              ];
              console.log('Messages after add:', newMessages.length);
              return newMessages;
            });
            currentMessageRef.current = '';
            setCurrentAssistantMessage('');
          } else {
            console.log('No content to save! currentMessageRef.current length:', currentMessageRef.current.length);
          }
          setIsStreaming(false);
        });

        service.onUIComponent((ui) => {
          // When a UI component is detected, add a message about it and set as current UI
          setCurrentUI(ui);
          setMessages((prev) => [
            ...prev,
            {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: '🎨 Building UI component...',
              timestamp: new Date()
            }
          ]);
          setIsStreaming(false);
        });

        service.onError((errorMsg) => {
          setError(errorMsg);
          setIsStreaming(false);
        });
      })
      .catch((err) => {
        console.error('SignalR initialization error:', err);
        setError(`Failed to connect to chat: ${err.message}. Retrying...`);
        // Don't permanently fail - the user can still send messages
      });

    return () => {
      service.leaveChat(chatId).catch(console.error);
      service.disconnect().catch(console.error);
    };
  }, [chatId]);

  const handleSend = async () => {
    if (!input.trim() || isStreaming) return;

    const userMessage: Message = {
      id: crypto.randomUUID(),
      role: 'user',
      content: input,
      timestamp: new Date()
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setIsStreaming(true);
    setCurrentAssistantMessage('');
    setError(null);

    try {
      await signalRService.current?.streamMessage(chatId, input);
    } catch (error) {
      console.error('Failed to send message:', error);
      setError('Failed to send message');
      setIsStreaming(false);
    }
  };

  const handleUIInteraction = async (componentId: string, payload: unknown) => {
    try {
      await signalRService.current?.submitUIInteraction(componentId, payload);
    } catch (error) {
      console.error('Failed to submit interaction:', error);
      setError('Failed to submit interaction');
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b px-4 py-3 shadow-sm">
        <h1 className="text-xl font-semibold text-gray-800">
          SemanticUI - FHIR Assistant
        </h1>
        <p className="text-sm text-gray-500">
          Chat ID: {chatId.substring(0, 8)}...
        </p>
      </div>

      {/* Main content area */}
      <div className="flex flex-1 overflow-hidden">
        {/* Chat Messages - Left side */}
        <div className="flex-1 flex flex-col">
          <div className="flex-1 overflow-y-auto p-4 space-y-4">
            {messages.map((message) => (
              <div
                key={message.id}
                className={`flex ${
                  message.role === 'user' ? 'justify-end' : 'justify-start'
                }`}
              >
                <div
                  className={`max-w-2xl rounded-lg p-4 ${
                    message.role === 'user'
                      ? 'bg-blue-500 text-white'
                      : 'bg-white shadow-sm border'
                  }`}
                >
                  <div className="whitespace-pre-wrap">{message.content}</div>
                  <div
                    className={`text-xs mt-2 ${
                      message.role === 'user' ? 'text-blue-100' : 'text-gray-400'
                    }`}
                  >
                    {new Date(message.timestamp).toLocaleTimeString()}
                  </div>
                </div>
              </div>
            ))}

            {/* Streaming assistant message */}
            {isStreaming && currentAssistantMessage && (
              <div className="flex justify-start">
                <div className="max-w-2xl bg-white rounded-lg p-4 shadow-sm border">
                  <div className="whitespace-pre-wrap">{currentAssistantMessage}</div>
                  <div className="flex space-x-1 mt-2">
                    <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce"></div>
                    <div
                      className="w-2 h-2 bg-blue-500 rounded-full animate-bounce"
                      style={{ animationDelay: '0.1s' }}
                    ></div>
                    <div
                      className="w-2 h-2 bg-blue-500 rounded-full animate-bounce"
                      style={{ animationDelay: '0.2s' }}
                    ></div>
                  </div>
                </div>
              </div>
            )}

            {/* Loading indicator */}
            {isStreaming && !currentAssistantMessage && (
              <div className="flex justify-start">
                <div className="bg-white rounded-lg p-4 shadow-sm border">
                  <div className="flex space-x-2">
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
                    <div
                      className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
                      style={{ animationDelay: '0.1s' }}
                    ></div>
                    <div
                      className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
                      style={{ animationDelay: '0.2s' }}
                    ></div>
                  </div>
                </div>
              </div>
            )}

            {/* Error message */}
            {error && (
              <div className="max-w-2xl mx-auto">
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                  <p className="text-red-800 font-semibold">Error</p>
                  <p className="text-red-600 text-sm mt-1">{error}</p>
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Input */}
          <div className="border-t bg-white p-4 shadow-lg">
            <div className="flex space-x-2 max-w-4xl mx-auto">
              <input
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder="Ask about patients, create forms, visualize data..."
                className="flex-1 border rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={isStreaming}
              />
              <button
                onClick={handleSend}
                disabled={isStreaming || !input.trim()}
                className="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
              >
                Send
              </button>
            </div>
            <div className="text-center mt-2">
              <p className="text-xs text-gray-500">
                Try: "Show me patient John Doe's information" or "Create a chart of
                age distribution"
              </p>
            </div>
          </div>
        </div>

        {/* UI Panel - Right side */}
        {currentUI && (
          <div className="w-96 border-l bg-white shadow-lg flex flex-col overflow-hidden">
            <div className="bg-gradient-to-r from-blue-500 to-blue-600 text-white p-4 flex justify-between items-center">
              <h2 className="font-semibold">Interactive Component</h2>
              <button
                onClick={() => setCurrentUI(null)}
                className="text-white hover:bg-blue-700 p-1 rounded transition-colors"
              >
                ✕
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-4">
              <DynamicUIRenderer
                uiDefinition={currentUI}
                onInteraction={(payload) =>
                  handleUIInteraction(currentUI.componentId, payload)
                }
                onError={(err) => setError(err)}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
