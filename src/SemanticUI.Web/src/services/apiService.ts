import { Chat } from '../types';

// Detect if running as Tauri desktop app or web
const isDesktopApp = typeof window !== 'undefined' && (window as any).__TAURI__ !== undefined;
const API_URL = isDesktopApp
  ? 'http://localhost:5050'
  : (import.meta.env.VITE_API_URL || 'http://localhost:5050');

// Get or generate user ID for session
function getUserId(): string {
  const cookieName = 'X-User-Id';
  const cookies = document.cookie.split(';').map(c => c.trim());

  for (const cookie of cookies) {
    if (cookie.startsWith(cookieName + '=')) {
      return cookie.substring(cookieName.length + 1);
    }
  }

  // If no cookie, generate a new one - it will be set by the server
  return crypto.randomUUID();
}

export const SessionUserService = {
  getUserId
};

export class ApiService {
  private static async fetch<T>(
    endpoint: string,
    options?: RequestInit
  ): Promise<T> {
    const response = await fetch(`${API_URL}${endpoint}`, {
      credentials: 'include', // Include cookies with every request
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers
      }
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.statusText}`);
    }

    return response.json();
  }

  static async getChats(): Promise<Chat[]> {
    const result = await this.fetch<{ success: boolean; data: Chat[] }>(
      '/api/chat'
    );
    return result.data;
  }

  static async getChat(chatId: string): Promise<Chat> {
    const result = await this.fetch<{ success: boolean; data: Chat }>(
      `/api/chat/${chatId}`
    );
    return result.data;
  }

  static async createChat(title?: string): Promise<Chat> {
    const result = await this.fetch<{ success: boolean; data: Chat }>(
      '/api/chat',
      {
        method: 'POST',
        body: JSON.stringify({ title })
      }
    );
    return result.data;
  }

  static async deleteChat(chatId: string): Promise<void> {
    await this.fetch(`/api/chat/${chatId}`, {
      method: 'DELETE'
    });
  }
}
