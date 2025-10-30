import React, { useState, useEffect } from 'react';
import { ChatInterface } from './components/ChatInterface';
import { ApiService } from './services/apiService';

function App() {
  const [chatId, setChatId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Initialize or load chat
    const initializeChat = async () => {
      try {
        // Check if there's a chat ID in URL params
        const urlParams = new URLSearchParams(window.location.search);
        const urlChatId = urlParams.get('chatId');

        if (urlChatId) {
          setChatId(urlChatId);
        } else {
          // Create a new chat
          const newChat = await ApiService.createChat('FHIR Chat');
          setChatId(newChat.id);
          // Update URL
          window.history.pushState(
            {},
            '',
            `?chatId=${newChat.id}`
          );
        }
      } catch (err) {
        setError(`Failed to initialize chat: ${(err as Error).message}`);
      } finally {
        setLoading(false);
      }
    };

    initializeChat();
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-b-2 border-blue-500 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading SemanticUI...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-screen bg-gray-50">
        <div className="max-w-md bg-white rounded-lg shadow-lg p-6">
          <h2 className="text-xl font-semibold text-red-600 mb-2">Error</h2>
          <p className="text-gray-700">{error}</p>
          <button
            onClick={() => window.location.reload()}
            className="mt-4 bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!chatId) {
    return (
      <div className="flex items-center justify-center h-screen bg-gray-50">
        <p className="text-gray-600">No chat ID available</p>
      </div>
    );
  }

  return <ChatInterface chatId={chatId} />;
}

export default App;
